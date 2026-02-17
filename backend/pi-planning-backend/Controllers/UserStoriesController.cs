using Microsoft.AspNetCore.Mvc;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId}/stories")]
    public class UserStoriesController : ControllerBase
    {
        private readonly IFeatureService _featureService;
        public UserStoriesController(IFeatureService featureService) => _featureService = featureService;

        // PATCH api/boards/{boardId}/stories/{storyId}/move
        [HttpPatch("{storyId}/move")]
        public async Task<IActionResult> MoveStory(int boardId, int storyId, [FromBody] MoveStoryDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            await _featureService.MoveUserStoryAsync(boardId, storyId, dto.TargetSprintId);
            return NoContent();
        }

        // PATCH api/boards/{boardId}/stories/{storyId}/refresh?org=&project=&pat=
        [HttpPatch("{storyId}/refresh")]
        public async Task<IActionResult> RefreshStory(int boardId, int storyId, [FromQuery] string organization, [FromQuery] string project, [FromQuery] string pat)
        {
            var s = await _featureService.RefreshUserStoryFromAzureAsync(boardId, storyId, organization, project, pat);
            if (s == null) return NotFound();
            return Ok(s);
        }
    }
}
