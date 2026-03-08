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

        /// <summary>
        /// Retrieves all team members with sprint capacity allocations.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <returns>List of team members with capacities.</returns>
        /// <response code="200">Team members retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<TeamMemberDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTeam([FromRoute] int boardId)
        {
            List<TeamMemberDto> team = await _service.GetTeamAsync(boardId);
            return Ok(team);
        }

        /// <summary>
        /// Adds a new team member to the board with default capacities for all sprints.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="member">Team member details (name, isDev, isTest).</param>
        /// <returns>Created team member with capacities.</returns>
        /// <response code="200">Team member added successfully.</response>
        /// <response code="403">Board is locked.</response>
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(TeamMemberResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddTeamMember([FromRoute] int boardId, [FromBody] TeamMemberDto member)
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

        /// <summary>
        /// Updates team member details (name, isDev, isTest). Re-calculates default capacities.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="teamMemberId">Team member ID.</param>
        /// <param name="member">Updated team member details.</param>
        /// <returns>Updated team member.</returns>
        /// <response code="200">Team member updated successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Team member not found.</response>
        [HttpPut("{teamMemberId}")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(TeamMemberResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTeamMember([FromRoute] int boardId, [FromRoute] int teamMemberId, [FromBody] TeamMemberDto member)
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

        /// <summary>
        /// Removes team member and all associated sprint capacity records.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="teamMemberId">Team member ID.</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Team member deleted successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Team member not found.</response>
        [HttpDelete("{teamMemberId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTeamMember([FromRoute] int boardId, [FromRoute] int teamMemberId)
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

        /// <summary>
        /// Adjusts a team member's capacity for a specific sprint.
        /// </summary>
        /// <param name="boardId">Board ID.</param>
        /// <param name="teamMemberId">Team member ID.</param>
        /// <param name="sprintId">Sprint ID.</param>
        /// <param name="dto">New capacity values (Dev and Test).</param>
        /// <returns>Updated capacity.</returns>
        /// <response code="200">Capacity updated successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Team member or sprint not found.</response>
        [HttpPatch("{teamMemberId}/sprints/{sprintId}")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(TeamMemberSprintDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCapacity([FromRoute] int boardId, [FromRoute] int teamMemberId, [FromRoute] int sprintId, [FromBody] UpdateTeamMemberCapacityDto dto)
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
