using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class AzureBoardsService : IAzureBoardsService
    {
        private readonly HttpClient _http;
        private readonly ILogger<AzureBoardsService> _log;

        public AzureBoardsService(HttpClient http, ILogger<AzureBoardsService> log)
        {
            _http = http;
            _log = log;
        }

        public async Task<FeatureDto> GetFeatureWithChildrenAsync(string organization, string project, int featureId, string pat)
        {
            var featureJson = await GetWorkItemWithRelationsAsync(organization, project, featureId, pat);

            var featureDto = ParseFeature(featureJson.Value);

            // gather child IDs
            var childIds = new List<int>();
            if (featureJson.Value.TryGetProperty("relations", out var relations))
            {
                foreach (var rel in relations.EnumerateArray())
                {
                    if (rel.GetProperty("rel").GetString() == "System.LinkTypes.Hierarchy-Forward")
                    {
                        var url = rel.GetProperty("url").GetString();
                        if (url != null)
                        {
                            var last = url.Split('/').Last();
                            if (int.TryParse(last, out var cid)) childIds.Add(cid);
                        }
                    }
                }
            }

            if (childIds.Count != 0)
            {
                var childrenJson = await GetWorkItemsAsync(organization, project, childIds, pat);
                if (childrenJson != null && childrenJson.Value.TryGetProperty("value", out var array))
                {
                    var children = new List<UserStoryDto>();
                    foreach (var wi in array.EnumerateArray())
                    {
                        var us = ParseUserStory(wi);
                        children.Add(us);
                    }
                    featureDto.Children = children;
                }
            }

            return featureDto;
        }

        public async Task<UserStoryDto> GetUserStoryAsync(string organization, string project, int userStoryId, string pat)
        {
            var userStoryJson = await GetWorkItemsAsync(organization, project, [userStoryId], pat);
            var userStory = ParseUserStory(userStoryJson.Value);
            return userStory;
        }

        #region Private Helpers
        private void SetPat(string pat)
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        private async Task<JsonElement?> GetWorkItemWithRelationsAsync(string organization, string project, int id, string pat)
        {
            SetPat(pat);
            var url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{id}?$expand=relations&api-version=7.2-preview.3";
            var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("Azure API failed: {Code} {Reason}", res.StatusCode, await res.Content.ReadAsStringAsync());
                return null;
            }

            var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement;
        }

        private async Task<JsonElement?> GetWorkItemsAsync(string organization, string project, IEnumerable<int> ids, string pat)
        {
            SetPat(pat);
            var idList = string.Join(",", ids);
            var url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems?ids={idList}&api-version=7.2-preview.3";
            var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode) return null;

            var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement;
        }

        private FeatureDto ParseFeature(JsonElement workItem)
        {
            var fields = workItem.GetProperty("fields");
            var id = workItem.GetProperty("id").GetInt32();
            var title = fields.GetProperty("System.Title").GetString() ?? "";
            var azureId = id.ToString();

            var feature = new FeatureDto { Id = id, Title = title, AzureId = azureId };

            return feature;
        }

        private UserStoryDto ParseUserStory(JsonElement workItem, string storyPointField = "Microsoft.VSTS.Scheduling.StoryPoints", string devField = "Custom.DevStoryPoints", string testField = "Custom.TestStoryPoints")
        {
            var fields = workItem.GetProperty("fields");
            var id = workItem.GetProperty("id").GetInt32();
            var title = fields.GetProperty("System.Title").GetString() ?? "";

            double? sp = null;
            if (fields.TryGetProperty(storyPointField, out var s) && s.ValueKind != JsonValueKind.Null)
                sp = s.GetDouble();

            double? dp = null;
            if (fields.TryGetProperty(devField, out var d) && d.ValueKind != JsonValueKind.Null)
                dp = d.GetDouble();

            double? tp = null;
            if (fields.TryGetProperty(testField, out var t) && t.ValueKind != JsonValueKind.Null)
                tp = t.GetDouble();

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
