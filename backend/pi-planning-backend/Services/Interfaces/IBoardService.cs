using System.Threading.Tasks;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface IBoardService
    {
        Task<Board> CreateBoardAsync(BoardCreateDto dto);
    }
}
