using System.Threading.Tasks;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface IBoardService
    {
        Task<Board> CreateBoardAsync(BoardCreateDto dto);
        Task<BoardResponseDto?> GetBoardWithHierarchyAsync(int boardId);
        Task<IEnumerable<BoardSummaryDto>> SearchBoardsAsync(string? searchTerm = null, string? organization = null, string? project = null, bool? isLocked = null, bool? isFinalized = null);
        Task<BoardSummaryDto?> GetBoardPreviewAsync(int boardId);
    }
}
