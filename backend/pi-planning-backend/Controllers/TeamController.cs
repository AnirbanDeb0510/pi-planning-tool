using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.DTOs.SignalR;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/boards/{boardId}/team")]
    public class TeamController(ITeamService service, IHubContext<PlanningHub> hubContext) : ControllerBase
    {
        private readonly ITeamService _service = service;
        private readonly IHubContext<PlanningHub> _hubContext = hubContext;

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

            TeamMemberAddedDto payload = new()
            {
                BoardId = boardId,
                TeamMember = created,
                TimestampUtc = DateTime.UtcNow
            };

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "TeamMemberAdded", payload, initiatorConnectionId);

            return Ok(created);
        }

        [HttpPut("{teamMemberId}")]
        public async Task<IActionResult> UpdateTeamMember(int boardId, int teamMemberId, [FromBody] TeamMemberDto member)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            TeamMemberResponseDto? updated = await _service.UpdateTeamMemberAsync(boardId, teamMemberId, member);
            if (updated == null)
            {
                return NotFound();
            }

            TeamMemberUpdatedDto payload = new()
            {
                BoardId = boardId,
                TeamMember = updated,
                TimestampUtc = DateTime.UtcNow
            };

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "TeamMemberUpdated", payload, initiatorConnectionId);

            return Ok(updated);
        }

        [HttpDelete("{teamMemberId}")]
        public async Task<IActionResult> DeleteTeamMember(int boardId, int teamMemberId)
        {
            bool deleted = await _service.DeleteTeamMemberAsync(boardId, teamMemberId);
            if (!deleted)
            {
                return NotFound();
            }

            TeamMemberDeletedDto payload = new()
            {
                BoardId = boardId,
                TeamMemberId = teamMemberId,
                TimestampUtc = DateTime.UtcNow
            };

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "TeamMemberDeleted", payload, initiatorConnectionId);

            return NoContent();
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

            CapacityUpdatedDto payload = new()
            {
                BoardId = boardId,
                TeamMemberId = teamMemberId,
                SprintId = sprintId,
                CapacityDev = responseDto.CapacityDev,
                CapacityTest = responseDto.CapacityTest,
                TimestampUtc = DateTime.UtcNow
            };

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, boardId, "CapacityUpdated", payload, initiatorConnectionId);

            return Ok(responseDto);
        }
    }
}
