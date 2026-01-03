using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;

namespace PiPlanningBackend.Repositories.Implementations
{
    public class TeamRepository : ITeamRepository
    {
        private readonly AppDbContext _db;

        public TeamRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<TeamMember>> GetTeamAsync(int boardId)
        {
            return await _db.TeamMembers
                .Where(t => t.BoardId == boardId)
                .Include(t => t.TeamMemberSprints)
                .ToListAsync();
        }

        public async Task AddTeamMemberAsync(TeamMember member)
        {
            await _db.TeamMembers.AddAsync(member);
        }

        public async Task AddAsync(int boardId, TeamMember member)
        {
            // Assign boardId if you store that in TeamMember
            member.BoardId = boardId;
            await _db.TeamMembers.AddAsync(member);
            await _db.SaveChangesAsync();
        }

        public async Task AddTeamMemberSprintAsync(TeamMemberSprint tms)
        {
            await _db.TeamMemberSprints.AddAsync(tms);
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

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
