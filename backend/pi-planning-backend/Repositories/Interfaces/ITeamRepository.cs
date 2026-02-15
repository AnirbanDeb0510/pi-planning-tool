using PiPlanningBackend.Models;

namespace PiPlanningBackend.Repositories.Interfaces
{
    public interface ITeamRepository
    {
        Task<List<TeamMember>> GetTeamAsync(int boardId);
        Task AddTeamMemberAsync(TeamMember member);
        Task AddTeamMemberSprintAsync(TeamMemberSprint tms);
        Task AddAsync(int boardId, TeamMember member);
        Task<TeamMember?> GetTeamMemberAsync(int memberId);
        Task<TeamMemberSprint?> GetTeamMemberSprintAsync(int sprintId, int memberId);
        Task DeleteTeamMemberAsync(TeamMember member);
        Task SaveChangesAsync();
    }
}
