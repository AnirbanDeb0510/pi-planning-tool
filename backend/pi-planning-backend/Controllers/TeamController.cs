using Microsoft.AspNetCore.Mvc;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId}/team")]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _service;

        public TeamController(ITeamService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetTeam(int boardId)
        {
            var team = await _service.GetTeamAsync(boardId);
            return Ok(team);
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdateTeam(int boardId, [FromBody] List<TeamMemberDto> members)
        {
            await _service.AddOrUpdateTeamAsync(boardId, members);
            return Ok();
        }

        [HttpPatch("sprints/{sprintId}/team/{teamMemberId}")]
        public async Task<IActionResult> UpdateCapacity(int boardId, int sprintId, int teamMemberId, [FromBody] double capacity)
        {
            var updated = await _service.UpdateCapacityAsync(sprintId, teamMemberId, capacity);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
    }
}
