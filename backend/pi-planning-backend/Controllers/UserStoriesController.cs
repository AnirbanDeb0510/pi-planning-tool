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

        // PATCH api/boards/{boardId}/stories/{storyId}/move
        [HttpPatch("{storyId}/move")]
        public async Task<IActionResult> MoveStory(int boardId, int storyId, [FromBody] MoveStoryDto dto)
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

            await _hubContext.Clients
                .Group(PlanningHub.GetBoardGroupName(boardId))
                .SendAsync("StoryMoved", payload);

            return NoContent();
        }

        // PATCH api/boards/{boardId}/stories/{storyId}/refresh?org=&project=&pat=
        [HttpPatch("{storyId}/refresh")]
        public async Task<IActionResult> RefreshStory(int boardId, int storyId, [FromQuery] string organization, [FromQuery] string project, [FromQuery] string pat)
        {
            UserStoryDto? s = await _featureService.RefreshUserStoryFromAzureAsync(boardId, storyId, organization, project, pat);
            return s == null ? NotFound() : Ok(s);
        }
    }
}
