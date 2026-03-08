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

        /// <summary>
        /// Imports a Feature (with child User Stories) from Azure DevOps into the board.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="dto">Feature data with children stories.</param>
        /// <returns>Created feature with assigned ID.</returns>
        /// <response code="201">Feature imported successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Board not found.</response>
        [HttpPost("import")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ImportFeature([FromRoute] int boardId, [FromBody] FeatureDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            FeatureDto created = await _featureService.ImportFeatureToBoardAsync(boardId, dto);

            FeatureImportedDto payload = new()
            {
                BoardId = boardId,
                Feature = created,
                TimestampUtc = DateTime.UtcNow
            };

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "FeatureImported", payload, initiatorConnectionId);

            return CreatedAtAction(nameof(GetFeature), new { boardId, id = created.Id }, created);
        }

        /// <summary>
        /// Placeholder endpoint for retrieving a single feature.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetFeature([FromRoute] int boardId, [FromRoute] int id)
        {
            // optional: implement retrieval controller or use other endpoint
            return Ok($"GetFeature endpoint hit for BoardId: {boardId}, FeatureId: {id}");
        }

        /// <summary>
        /// Re-fetches Feature and children from Azure DevOps, updating local data.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="id">Feature ID.</param>
        /// <param name="organization">Azure DevOps organization.</param>
        /// <param name="project">Azure DevOps project.</param>
        /// <param name="pat">Personal Access Token.</param>
        /// <returns>Updated feature data.</returns>
        /// <response code="200">Feature refreshed successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Feature not found.</response>
        [HttpPatch("{id}/refresh")]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RefreshFeature([FromRoute] int boardId, [FromRoute] int id, [FromQuery] string organization, [FromQuery] string project, [FromQuery] string pat)
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

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "FeatureRefreshed", payload, initiatorConnectionId);

            return Ok(featureDto);
        }

        /// <summary>
        /// Updates priority order for multiple features atomically.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="dto">List of feature IDs with new priorities.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Features reordered successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Board not found.</response>
        [HttpPatch("reorder")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReorderFeatures([FromRoute] int boardId, [FromBody] ReorderFeatureDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            await _featureService.ReorderFeaturesAsync(boardId, dto.Features);

            FeaturesReorderedDto payload = new()
            {
                BoardId = boardId,
                Features = dto.Features,
                TimestampUtc = DateTime.UtcNow
            };

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "FeaturesReordered", payload, initiatorConnectionId);

            return NoContent();
        }

        /// <summary>
        /// Deletes a feature and all its child User Stories from the board.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="id">Feature ID.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Feature deleted successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Feature not found.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFeature([FromRoute] int boardId, [FromRoute] int id)
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

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "FeatureDeleted", payload, initiatorConnectionId);

            return NoContent();
        }
    }
}
