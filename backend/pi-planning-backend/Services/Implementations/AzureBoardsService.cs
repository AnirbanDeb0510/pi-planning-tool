using System.Net.Http.Headers;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class AzureBoardsService(HttpClient http, ILogger<AzureBoardsService> log, IBoardService boardService) : IAzureBoardsService
    {
        private const string DefaultStoryPointField = "Microsoft.VSTS.Scheduling.StoryPoints";
        private const string DefaultDevField = "Custom.DevStoryPoints";
        private const string DefaultTestField = "Custom.TestStoryPoints";

        private readonly HttpClient _http = http;
        private readonly ILogger<AzureBoardsService> _log = log;
        private readonly IBoardService _boardService = boardService;

        public async Task<FeatureDto> GetFeatureWithChildrenForBoardAsync(int boardId, int featureId, string pat)
        {
            Board board = await _boardService.GetBoardAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            return string.IsNullOrWhiteSpace(board.Organization) || string.IsNullOrWhiteSpace(board.Project)
                ? throw new ArgumentException("Board Azure configuration is incomplete. Organization and Project are required.")
                : await GetFeatureWithChildrenAsync(
                board.Organization,
                board.Project,
                featureId,
                pat,
                board.AzureStoryPointField,
                board.AzureDevStoryPointField,
                board.AzureTestStoryPointField);
        }

        public async Task<FeatureDto> GetFeatureWithChildrenAsync(
            string organization,
            string project,
            int featureId,
            string pat,
            string? storyPointField = null,
            string? devField = null,
            string? testField = null)
        {
            string resolvedStoryPointField = ResolveField(storyPointField, DefaultStoryPointField);
            string resolvedDevField = ResolveField(devField, DefaultDevField);
            string resolvedTestField = ResolveField(testField, DefaultTestField);

            JsonElement featureJson = await GetWorkItemWithRelationsAsync(organization, project, featureId, pat);

            FeatureDto featureDto = ParseFeature(featureJson);

            // gather child IDs
            List<int> childIds = [];
            if (featureJson.TryGetProperty("relations", out JsonElement relations))
            {
                foreach (JsonElement rel in relations.EnumerateArray())
                {
                    if (rel.GetProperty("rel").GetString() == "System.LinkTypes.Hierarchy-Forward")
                    {
                        string? url = rel.GetProperty("url").GetString();
                        if (url != null)
                        {
                            string last = url.Split('/').Last();
                            if (int.TryParse(last, out int cid))
                            {
                                childIds.Add(cid);
                            }
                        }
                    }
                }
            }

            if (childIds.Count != 0)
            {
                JsonElement childrenJson = await GetWorkItemsAsync(organization, project, childIds, pat);
                if (childrenJson.TryGetProperty("value", out JsonElement array))
                {
                    List<UserStoryDto> children = [];
                    foreach (JsonElement wi in array.EnumerateArray())
                    {
                        UserStoryDto us = ParseUserStory(wi, resolvedStoryPointField, resolvedDevField, resolvedTestField);
                        children.Add(us);
                    }
                    featureDto.Children = children;
                }
            }

            return featureDto;
        }

        public async Task<UserStoryDto> GetUserStoryAsync(
            string organization,
            string project,
            int userStoryId,
            string pat,
            string? storyPointField = null,
            string? devField = null,
            string? testField = null)
        {
            string resolvedStoryPointField = ResolveField(storyPointField, DefaultStoryPointField);
            string resolvedDevField = ResolveField(devField, DefaultDevField);
            string resolvedTestField = ResolveField(testField, DefaultTestField);

            JsonElement userStoryJson = await GetWorkItemsAsync(organization, project, [userStoryId], pat);
            if (!userStoryJson.TryGetProperty("value", out JsonElement valueArray)
                || valueArray.ValueKind != JsonValueKind.Array
                || valueArray.GetArrayLength() == 0)
            {
                throw new KeyNotFoundException($"Azure user story with ID {userStoryId} not found.");
            }

            UserStoryDto userStory = ParseUserStory(valueArray[0], resolvedStoryPointField, resolvedDevField, resolvedTestField);
            return userStory;
        }

        #region Private Helpers
        private void SetPat(string pat)
        {
            string authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        private async Task<JsonElement> GetWorkItemWithRelationsAsync(string organization, string project, int id, string pat)
        {
            SetPat(pat);
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{id}?$expand=relations&api-version=7.2-preview.3";
            HttpResponseMessage res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning(
                    "Azure work item fetch failed | StatusCode: {StatusCode} | Organization: {Organization} | Project: {Project} | WorkItemId: {WorkItemId}",
                    res.StatusCode,
                    organization,
                    project,
                    id);

                throw CreateAzureException(res.StatusCode, $"work item {id}");
            }

            JsonDocument doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement;
        }

        private async Task<JsonElement> GetWorkItemsAsync(string organization, string project, IEnumerable<int> ids, string pat)
        {
            SetPat(pat);
            string idList = string.Join(",", ids);
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems?ids={idList}&api-version=7.2-preview.3";
            HttpResponseMessage res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning(
                    "Azure work items fetch failed | StatusCode: {StatusCode} | Organization: {Organization} | Project: {Project} | WorkItemIds: {WorkItemIds}",
                    res.StatusCode,
                    organization,
                    project,
                    idList);

                throw CreateAzureException(res.StatusCode, $"work items [{idList}]");
            }

            JsonDocument doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement;
        }

        private static Exception CreateAzureException(HttpStatusCode statusCode, string operation)
        {

            return statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? new AuthenticationException("Azure PAT is invalid, expired, or does not have the required permissions.")
                : statusCode is HttpStatusCode.NotFound
                ? new KeyNotFoundException($"Azure resource not found for {operation}.")
                : statusCode is HttpStatusCode.TooManyRequests
                ? new HttpRequestException("Azure Boards rate limit exceeded. Please retry shortly.", null, statusCode)
                : new HttpRequestException(
                                            $"Azure API request failed for {operation} with status {(int)statusCode} ({statusCode}).",
                                            null,
                                            statusCode);
        }

        private static FeatureDto ParseFeature(JsonElement workItem)
        {
            JsonElement fields = workItem.GetProperty("fields");
            int id = workItem.GetProperty("id").GetInt32();
            string title = fields.GetProperty("System.Title").GetString() ?? "";
            string azureId = id.ToString();

            FeatureDto feature = new() { Id = id, Title = title, AzureId = azureId };

            return feature;
        }

        private static string ResolveField(string? configuredField, string fallback)
        {
            return string.IsNullOrWhiteSpace(configuredField) ? fallback : configuredField;
        }

        private static UserStoryDto ParseUserStory(JsonElement workItem, string storyPointField, string devField, string testField)
        {
            JsonElement fields = workItem.GetProperty("fields");
            int id = workItem.GetProperty("id").GetInt32();
            string title = fields.GetProperty("System.Title").GetString() ?? "";

            double? sp = null;
            if (fields.TryGetProperty(storyPointField, out JsonElement s) && s.ValueKind != JsonValueKind.Null)
            {
                sp = s.GetDouble();
            }

            double? dp = null;
            if (fields.TryGetProperty(devField, out JsonElement d) && d.ValueKind != JsonValueKind.Null)
            {
                dp = d.GetDouble();
            }

            double? tp = null;
            if (fields.TryGetProperty(testField, out JsonElement t) && t.ValueKind != JsonValueKind.Null)
            {
                tp = t.GetDouble();
            }

            return new UserStoryDto
            {
                Id = id,
                Title = title,
                AzureId = id.ToString(),
                StoryPoints = sp,
                DevStoryPoints = dp,
                TestStoryPoints = tp
            };
        }

        #endregion
    }
}
