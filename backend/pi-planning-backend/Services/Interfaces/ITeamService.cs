using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface ITeamService
    {
        Task<List<TeamMemberDto>> GetTeamAsync(int boardId);
        Task<TeamMemberResponseDto> AddTeamMemberAsync(int boardId, TeamMemberDto member);
        Task<TeamMemberResponseDto?> UpdateTeamMemberAsync(int boardId, int memberId, TeamMemberDto member);
        Task<bool> DeleteTeamMemberAsync(int boardId, int memberId);
        Task<TeamMemberSprint?> UpdateCapacityAsync(int boardId, int sprintId, int teamMemberId, UpdateTeamMemberCapacityDto dto);
    }
}
