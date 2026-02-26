using Microsoft.AspNetCore.Mvc;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId}/team")]
    public class TeamController(ITeamService service) : ControllerBase
    {
        private readonly ITeamService _service = service;

        [HttpGet]
        public async Task<IActionResult> GetTeam(int boardId)
        {
            List<TeamMemberDto> team = await _service.GetTeamAsync(boardId);
            return Ok(team);
        }

        [HttpPost]
        public async Task<IActionResult> AddTeamMember(int boardId, [FromBody] TeamMemberDto member)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            TeamMemberResponseDto created = await _service.AddTeamMemberAsync(boardId, member);
            return Ok(created);
        }

        [HttpPut("{teamMemberId}")]
        public async Task<IActionResult> UpdateTeamMember(int boardId, int teamMemberId, [FromBody] TeamMemberDto member)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            TeamMemberResponseDto? updated = await _service.UpdateTeamMemberAsync(boardId, teamMemberId, member);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{teamMemberId}")]
        public async Task<IActionResult> DeleteTeamMember(int boardId, int teamMemberId)
        {
            bool deleted = await _service.DeleteTeamMemberAsync(boardId, teamMemberId);
            return !deleted ? NotFound() : NoContent();
        }

        [HttpPatch("{teamMemberId}/sprints/{sprintId}")]
        public async Task<IActionResult> UpdateCapacity(int boardId, int teamMemberId, int sprintId, [FromBody] UpdateTeamMemberCapacityDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            TeamMemberSprint? updated = await _service.UpdateCapacityAsync(boardId, sprintId, teamMemberId, dto);
            if (updated == null)
            {
                return NotFound();
            }

            // Map to DTO to avoid circular reference serialization issues
            TeamMemberSprintDto responseDto = new()
            {
                SprintId = updated.SprintId,
                CapacityDev = updated.CapacityDev,
                CapacityTest = updated.CapacityTest
            };

            return Ok(responseDto);
        }
    }
}
