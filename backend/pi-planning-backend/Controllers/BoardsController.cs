using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.DTOs.SignalR;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoardsController(IBoardService boardService, IHubContext<PlanningHub> hubContext) : ControllerBase
    {
        private readonly IBoardService _boardService = boardService;
        private readonly IHubContext<PlanningHub> _hubContext = hubContext;

        [HttpPost]
        public async Task<IActionResult> CreateBoard(BoardCreateDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            Board board = await _boardService.CreateBoardAsync(dto);

            BoardCreatedDto response = new()
            {
                Id = board.Id,
                Name = board.Name,
                Organization = board.Organization,
                Project = board.Project,
                NumSprints = board.NumSprints,
                SprintDuration = board.SprintDuration,
                StartDate = board.StartDate,
                IsLocked = board.IsLocked,
                IsFinalized = board.IsFinalized,
                DevTestToggle = board.DevTestToggle,
                CreatedAt = board.CreatedAt
            };

            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBoard(int id)
        {
            BoardResponseDto? board = await _boardService.GetBoardWithHierarchyAsync(id);
            return board == null ? NotFound() : Ok(board);
        }

        [HttpGet]
        public async Task<IActionResult> SearchBoards(
            [FromQuery] string? search,
            [FromQuery][BindRequired] string organization,
            [FromQuery][BindRequired] string project,
            [FromQuery] bool? isLocked,
            [FromQuery] bool? isFinalized)
        {
            IEnumerable<BoardSummaryDto> boards = await _boardService.SearchBoardsAsync(search, organization.Trim(), project.Trim(), isLocked, isFinalized);
            return Ok(boards);
        }

        [HttpGet("{id}/preview")]
        public async Task<IActionResult> GetBoardPreview(int id)
        {
            BoardSummaryDto? preview = await _boardService.GetBoardPreviewAsync(id);
            return preview == null ? NotFound() : Ok(preview);
        }

        [HttpGet("{id}/validate-finalization")]
        public async Task<IActionResult> ValidateBoardForFinalization(int id)
        {
            (_, List<string> warnings) = await _boardService.ValidateBoardForFinalizationAsync(id);
            return Ok(warnings);
        }

        [HttpPatch("{id}/finalize")]
        public async Task<IActionResult> FinalizeBoard(int id)
        {
            // Validate board can be finalized
            (bool canFinalize, List<string> warnings) = await _boardService.ValidateBoardForFinalizationAsync(id);

            if (!canFinalize)
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = "Board cannot be finalized",
                        warnings,
                        timestamp = DateTime.UtcNow
                    }
                });
            }

            // Attempt to finalize
            BoardSummaryDto? board = await _boardService.FinalizeBoardAsync(id);
            if (board == null)
            {
                return NotFound();
            }

            BoardFinalizedDto payload = new()
            {
                BoardId = id,
                IsFinalized = true,
                TimestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(PlanningHub.GetBoardGroupName(id))
                .SendAsync("BoardFinalized", payload);

            return Ok(new
            {
                success = true,
                message = warnings.Count != 0 ? $"Board finalized with {warnings.Count} warning(s)" : "Board finalized successfully",
                board,
                warnings,
                finalizedAt = DateTime.UtcNow,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreBoard(int id)
        {
            BoardSummaryDto? board = await _boardService.RestoreBoardAsync(id);
            if (board == null)
            {
                return NotFound();
            }

            BoardRestoredDto payload = new()
            {
                BoardId = id,
                IsFinalized = false,
                TimestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(PlanningHub.GetBoardGroupName(id))
                .SendAsync("BoardRestored", payload);

            return Ok(new
            {
                success = true,
                message = "Board restored - editing is now allowed",
                board,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
