using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;

namespace PiPlanningBackend.Repositories.Implementations
{
    public class BoardRepository : IBoardRepository
    {
        private readonly AppDbContext _context;

        public BoardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Board> AddAsync(Board board)
        {
            _context.Boards.Add(board);
            await _context.SaveChangesAsync();
            return board;
        }

        public async Task<Board?> GetByIdAsync(int id)
        {
            return await _context.Boards
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Board>> GetAllAsync()
        {
            return await _context.Boards
                .Include(b => b.Sprints)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Board?> GetBoardWithSprintsAsync(int boardId)
        {
            return await _context.Boards
                .Include(b => b.Sprints)
                .FirstOrDefaultAsync(b => b.Id == boardId);
        }

        public async Task<Board?> GetBoardWithFullHierarchyAsync(int boardId)
        {
            return await _context.Boards
                .Include(b => b.Sprints)
                .Include(b => b.Features)
                    .ThenInclude(f => f.UserStories)
                .Include(b => b.TeamMembers)  // Add if not present
                    .ThenInclude(tm => tm.TeamMemberSprints)
                .FirstOrDefaultAsync(b => b.Id == boardId);
        }

        public async Task<Board?> GetBoardWithFeaturesAsync(int boardId)
        {
            return await _context.Boards
                .Include(b => b.Features)
                .FirstOrDefaultAsync(b => b.Id == boardId);
        }

        public async Task<IEnumerable<Board>> SearchBoardsAsync(string? searchTerm = null, string? organization = null, string? project = null, bool? isLocked = null, bool? isFinalized = null)
        {
            var query = _context.Boards
                .Include(b => b.Sprints)
                .Include(b => b.Features)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(b => b.Name.Contains(searchTerm));

            if (!string.IsNullOrWhiteSpace(organization))
                query = query.Where(b => b.Organization == organization);

            if (!string.IsNullOrWhiteSpace(project))
                query = query.Where(b => b.Project == project);

            if (isLocked.HasValue)
                query = query.Where(b => b.IsLocked == isLocked.Value);

            if (isFinalized.HasValue)
                query = query.Where(b => b.IsFinalized == isFinalized.Value);

            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }
    }
}
