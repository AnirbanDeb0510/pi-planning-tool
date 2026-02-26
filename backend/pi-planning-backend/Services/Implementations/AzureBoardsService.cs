using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class AzureBoardsService(HttpClient http, ILogger<AzureBoardsService> log) : IAzureBoardsService
    {
        private readonly HttpClient _http = http;
        private readonly ILogger<AzureBoardsService> _log = log;

        public async Task<FeatureDto> GetFeatureWithChildrenAsync(string organization, string project, int featureId, string pat)
        {
            JsonElement? featureJson = await GetWorkItemWithRelationsAsync(organization, project, featureId, pat);

            FeatureDto featureDto = ParseFeature(featureJson.Value);

            // gather child IDs
            List<int> childIds = [];
            if (featureJson.Value.TryGetProperty("relations", out JsonElement relations))
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
                JsonElement? childrenJson = await GetWorkItemsAsync(organization, project, childIds, pat);
                if (childrenJson != null && childrenJson.Value.TryGetProperty("value", out JsonElement array))
                {
                    List<UserStoryDto> children = [];
                    foreach (JsonElement wi in array.EnumerateArray())
                    {
                        UserStoryDto us = ParseUserStory(wi);
                        children.Add(us);
                    }
                    featureDto.Children = children;
                }
            }

            return featureDto;
        }

        public async Task<UserStoryDto> GetUserStoryAsync(string organization, string project, int userStoryId, string pat)
        {
            JsonElement? userStoryJson = await GetWorkItemsAsync(organization, project, [userStoryId], pat);
            UserStoryDto userStory = ParseUserStory(userStoryJson.Value);
            return userStory;
        }

        #region Private Helpers
        private void SetPat(string pat)
        {
            string authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        private async Task<JsonElement?> GetWorkItemWithRelationsAsync(string organization, string project, int id, string pat)
        {
            SetPat(pat);
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{id}?$expand=relations&api-version=7.2-preview.3";
            HttpResponseMessage res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("Azure API failed: {Code} {Reason}", res.StatusCode, await res.Content.ReadAsStringAsync());
                return null;
            }

            JsonDocument doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement;
        }

        private async Task<JsonElement?> GetWorkItemsAsync(string organization, string project, IEnumerable<int> ids, string pat)
        {
            SetPat(pat);
            string idList = string.Join(",", ids);
            string url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems?ids={idList}&api-version=7.2-preview.3";
            HttpResponseMessage res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }

            JsonDocument doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement;
        }

        private FeatureDto ParseFeature(JsonElement workItem)
        {
            JsonElement fields = workItem.GetProperty("fields");
            int id = workItem.GetProperty("id").GetInt32();
            string title = fields.GetProperty("System.Title").GetString() ?? "";
            string azureId = id.ToString();

            FeatureDto feature = new() { Id = id, Title = title, AzureId = azureId };

            return feature;
        }

        private UserStoryDto ParseUserStory(JsonElement workItem, string storyPointField = "Microsoft.VSTS.Scheduling.StoryPoints", string devField = "Custom.DevStoryPoints", string testField = "Custom.TestStoryPoints")
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
