using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services
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

        private void SetPat(string pat)
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        public async Task<JsonElement?> GetWorkItemWithRelationsAsync(string organization, string project, int id, string pat)
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

        public async Task<JsonElement?> GetWorkItemsAsync(string organization, string project, IEnumerable<int> ids, string pat)
        {
            SetPat(pat);
            var idList = string.Join(",", ids);
            var url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems?ids={idList}&api-version=7.2-preview.3";
            var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode) return null;

            var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement;
        }

        public FeatureDto ParseFeature(JsonElement workItem)
        {
            var fields = workItem.GetProperty("fields");
            var id = workItem.GetProperty("id").GetInt32();
            var title = fields.GetProperty("System.Title").GetString() ?? "";
            var azureId = id.ToString();

            var feature = new FeatureDto { Id = id, Title = title, AzureId = azureId };

            return feature;
        }

        public UserStoryDto ParseUserStory(JsonElement workItem, string storyPointField, string devField, string testField)
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
    }
}
