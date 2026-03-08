using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.DTOs.SignalR;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId}/stories")]
    public class UserStoriesController(IFeatureService featureService, IHubContext<PlanningHub> hubContext) : ControllerBase
    {
        private readonly IFeatureService _featureService = featureService;
        private readonly IHubContext<PlanningHub> _hubContext = hubContext;

        /// <summary>
        /// Assigns a User Story to a specific sprint (or Placeholder with null).
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="storyId">User Story ID.</param>
        /// <param name="dto">Target sprint ID (null for Placeholder).</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Story moved successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Story or sprint not found.</response>
        [HttpPatch("{storyId}/move")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MoveStory([FromRoute] int boardId, [FromRoute] int storyId, [FromBody] MoveStoryDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            await _featureService.MoveUserStoryAsync(boardId, storyId, dto.TargetSprintId);

            StoryMovedDto payload = new()
            {
                BoardId = boardId,
                StoryId = storyId,
                TargetSprintId = dto.TargetSprintId,
                TimestampUtc = DateTime.UtcNow
            };

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "StoryMoved", payload, initiatorConnectionId);

            return NoContent();
        }

        /// <summary>
        /// Re-fetches User Story details from Azure DevOps.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="storyId">User Story ID.</param>
        /// <param name="organization">Azure DevOps organization.</param>
        /// <param name="project">Azure DevOps project.</param>
        /// <param name="pat">Personal Access Token.</param>
        /// <returns>Updated story data.</returns>
        /// <response code="200">Story refreshed successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Story not found.</response>
        [HttpPatch("{storyId}/refresh")]
        [ProducesResponseType(typeof(UserStoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RefreshStory([FromRoute] int boardId, [FromRoute] int storyId, [FromQuery] string organization, [FromQuery] string project, [FromQuery] string pat)
        {
            UserStoryDto? s = await _featureService.RefreshUserStoryFromAzureAsync(boardId, storyId, organization, project, pat);
            if (s == null)
            {
                return NotFound();
            }

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "StoryRefreshed", s, initiatorConnectionId);

            return Ok(s);
        }
    }
}
