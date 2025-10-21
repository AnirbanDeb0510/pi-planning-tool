using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface ITeamService
    {
        Task<List<TeamMemberDto>> GetTeamAsync(int boardId);
        Task AddOrUpdateTeamAsync(int boardId, List<TeamMemberDto> members);
        Task<TeamMemberSprint?> UpdateCapacityAsync(int sprintId, int teamMemberId, double capacity);
    }
}
