using PiPlanningBackend.Models;

namespace PiPlanningBackend.Repositories.Interfaces
{
    public interface IBoardRepository
    {
        Task<Board> AddAsync(Board board);
        Task<Board?> GetByIdAsync(int id);
        Task<Board?> GetBoardWithSprintsAsync(int boardId);
        Task<IEnumerable<Board>> GetAllAsync();
        Task<IEnumerable<Board>> SearchBoardsAsync(string? searchTerm = null, string? organization = null, string? project = null, bool? isLocked = null, bool? isFinalized = null);
        Task SaveChangesAsync();
        Task<Board?> GetBoardWithFullHierarchyAsync(int boardId);
        Task<Board?> GetBoardWithFeaturesAsync(int boardId);
    }
}
