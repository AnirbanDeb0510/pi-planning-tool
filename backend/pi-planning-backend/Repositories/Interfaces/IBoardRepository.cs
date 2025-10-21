using PiPlanningBackend.Models;

namespace PiPlanningBackend.Repositories.Interfaces
{
    public interface IBoardRepository
    {
        Task<Board> AddAsync(Board board);
        Task<Board?> GetByIdAsync(int id);
        Task<IEnumerable<Board>> GetAllAsync();
        Task SaveChangesAsync();
    }
}
