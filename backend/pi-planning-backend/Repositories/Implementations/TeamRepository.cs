using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;

namespace PiPlanningBackend.Repositories.Implementations
{
    public class TeamRepository(AppDbContext db) : ITeamRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<List<TeamMember>> GetTeamAsync(int boardId)
        {
            return await _db.TeamMembers
                .Where(t => t.BoardId == boardId)
                .Include(t => t.TeamMemberSprints)
                .ToListAsync();
        }

        public async Task AddTeamMemberAsync(TeamMember member)
        {
            _ = await _db.TeamMembers.AddAsync(member);
        }

        public async Task AddAsync(int boardId, TeamMember member)
        {
            // Assign boardId if you store that in TeamMember
            member.BoardId = boardId;
            _ = await _db.TeamMembers.AddAsync(member);
            _ = await _db.SaveChangesAsync();
        }

        public async Task AddTeamMemberSprintAsync(TeamMemberSprint tms)
        {
            _ = await _db.TeamMemberSprints.AddAsync(tms);
        }

        public async Task<TeamMember?> GetTeamMemberAsync(int memberId)
        {
            return await _db.TeamMembers
                .Include(t => t.TeamMemberSprints)
                .FirstOrDefaultAsync(t => t.Id == memberId);
        }

        public async Task<TeamMemberSprint?> GetTeamMemberSprintAsync(int sprintId, int memberId)
        {
            return await _db.TeamMemberSprints
                .Include(t => t.TeamMember)
                .Include(t => t.Sprint)
                    .ThenInclude(s => s.Board)
                .FirstOrDefaultAsync(t => t.SprintId == sprintId && t.TeamMemberId == memberId);
        }

        public Task DeleteTeamMemberAsync(TeamMember member)
        {
            _ = _db.TeamMembers.Remove(member);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            _ = await _db.SaveChangesAsync();
        }
    }
}
