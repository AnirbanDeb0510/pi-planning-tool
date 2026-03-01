using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.DTOs.SignalR;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/v1/boards/{boardId}/features")]
    public class FeaturesController(IFeatureService featureService, IHubContext<PlanningHub> hubContext) : ControllerBase
    {
        private readonly IFeatureService _featureService = featureService;
        private readonly IHubContext<PlanningHub> _hubContext = hubContext;

        // POST api/v1/boards/{boardId}/features/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportFeature(int boardId, [FromBody] FeatureDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            FeatureDto created = await _featureService.ImportFeatureToBoardAsync(boardId, dto);

            FeatureImportedDto payload = new()
            {
                BoardId = boardId,
                Feature = created,
                TimestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(PlanningHub.GetBoardGroupName(boardId))
                .SendAsync("FeatureImported", payload);

            return CreatedAtAction(nameof(GetFeature), new { boardId, id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public IActionResult GetFeature(int boardId, int id)
        {
            // optional: implement retrieval controller or use other endpoint
            return Ok($"GetFeature endpoint hit for BoardId: {boardId}, FeatureId: {id}");
        }

        // PATCH api/boards/{boardId}/features/{id}/refresh?org=&project=&pat=
        [HttpPatch("{id}/refresh")]
        public async Task<IActionResult> RefreshFeature(int boardId, int id, [FromQuery] string organization, [FromQuery] string project, [FromQuery] string pat)
        {
            FeatureDto? featureDto = await _featureService.RefreshFeatureFromAzureAsync(boardId, id, organization, project, pat);
            if (featureDto == null)
            {
                return NotFound();
            }

            FeatureRefreshedDto payload = new()
            {
                BoardId = boardId,
                Feature = featureDto,
                TimestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(PlanningHub.GetBoardGroupName(boardId))
                .SendAsync("FeatureRefreshed", payload);

            return Ok(featureDto);
        }

        // PATCH api/boards/{boardId}/features/reorder
        [HttpPatch("reorder")]
        public async Task<IActionResult> ReorderFeatures(int boardId, [FromBody] ReorderFeatureDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            await _featureService.ReorderFeaturesAsync(boardId, dto.Features);

            FeaturesReorderedDto payload = new()
            {
                BoardId = boardId,
                Features = dto.Features,
                TimestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(PlanningHub.GetBoardGroupName(boardId))
                .SendAsync("FeaturesReordered", payload);

            return NoContent();
        }

        // DELETE api/boards/{boardId}/features/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeature(int boardId, int id)
        {
            bool deleted = await _featureService.DeleteFeatureAsync(boardId, id);
            if (!deleted)
            {
                return NotFound();
            }

            FeatureDeletedDto payload = new()
            {
                BoardId = boardId,
                FeatureId = id,
                TimestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(PlanningHub.GetBoardGroupName(boardId))
                .SendAsync("FeatureDeleted", payload);

            return NoContent();
        }
    }
}
