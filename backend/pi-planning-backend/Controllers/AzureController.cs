using Microsoft.AspNetCore.Mvc;
using PiPlanningBackend.Services;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/azure")]
    public class AzureController : ControllerBase
    {
        private readonly IAzureBoardsService _azure;
        private readonly IHttpClientFactory _httpFactory;

        public AzureController(IAzureBoardsService azure, IHttpClientFactory httpFactory)
        {
            _azure = azure;
            _httpFactory = httpFactory;
        }

        // GET api/azure/feature/{organization}/{project}/{featureId}?pat=xxxx
        [HttpGet("feature/{organization}/{project}/{featureId}")]
        public async Task<IActionResult> GetFeatureWithChildren(string organization, string project, int featureId, [FromQuery] string pat)
        {
            if (string.IsNullOrWhiteSpace(pat)) return BadRequest("PAT required as ?pat=...");

            // fetch feature (expand relations)
            var featureJson = await _azure.GetWorkItemWithRelationsAsync(organization, project, featureId, pat);
            if (featureJson == null) return StatusCode(502, "Azure API error getting feature");

            var featureDto = _azure.ParseFeature(featureJson.Value);

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

            if (childIds.Any())
            {
                var childrenJson = await _azure.GetWorkItemsAsync(organization, project, childIds, pat);
                if (childrenJson != null && childrenJson.Value.TryGetProperty("value", out var array))
                {
                    var children = new List<UserStoryDto>();
                    foreach (var wi in array.EnumerateArray())
                    {
                        var us = _azure.ParseUserStory(wi);
                        children.Add(us);
                    }
                    featureDto.Children = children;
                }
            }

            return Ok(featureDto);
        }
    }
}
