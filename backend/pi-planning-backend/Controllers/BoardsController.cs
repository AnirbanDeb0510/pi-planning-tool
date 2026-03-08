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

        /// <summary>
        /// Creates a new PI planning board with auto-generated sprints.
        /// </summary>
        /// <param name="dto">Board creation details including name, organization, project, and sprint configuration.</param>
        /// <returns>Created board with ID and metadata.</returns>
        /// <response code="201">Board created successfully.</response>
        /// <response code="400">Validation error (invalid field values).</response>
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(BoardCreatedDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateBoard([FromBody] BoardCreateDto dto)
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

        /// <summary>
        /// Retrieves complete board data including sprints, features, user stories, and team members.
        /// </summary>
        /// <param name="id">Board ID.</param>
        /// <returns>Full board hierarchy.</returns>
        /// <response code="200">Board found and returned.</response>
        /// <response code="404">Board not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BoardResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBoard([FromRoute] int id)
        {
            BoardResponseDto? board = await _boardService.GetBoardWithHierarchyAsync(id);
            return board == null ? NotFound() : Ok(board);
        }

        /// <summary>
        /// Searches and filters boards by organization, project, and optional criteria.
        /// </summary>
        /// <param name="search">Optional board name filter (case-insensitive partial match).</param>
        /// <param name="organization">Azure DevOps organization (required).</param>
        /// <param name="project">Azure DevOps project (required).</param>
        /// <param name="isLocked">Optional filter by lock status.</param>
        /// <param name="isFinalized">Optional filter by finalization status.</param>
        /// <returns>List of matching boards.</returns>
        /// <response code="200">Boards retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BoardSummaryDto>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Retrieves lightweight board metadata without loading full hierarchy (features, stories, team).
        /// </summary>
        /// <param name="id">Board ID.</param>
        /// <returns>Board summary with lock/finalization status.</returns>
        /// <response code="200">Board preview retrieved.</response>
        /// <response code="404">Board not found.</response>
        [HttpGet("{id}/preview")]
        [ProducesResponseType(typeof(BoardSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBoardPreview([FromRoute] int id)
        {
            BoardSummaryDto? preview = await _boardService.GetBoardPreviewAsync(id);
            return preview == null ? NotFound() : Ok(preview);
        }

        /// <summary>
        /// Returns warnings if board has unassigned stories or capacity issues.
        /// </summary>
        /// <param name="id">Board ID.</param>
        /// <returns>List of warning messages (empty if ready for finalization).</returns>
        /// <response code="200">Validation complete.</response>
        [HttpGet("{id}/validate-finalization")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateBoardForFinalization([FromRoute] int id)
        {
            (_, List<string> warnings) = await _boardService.ValidateBoardForFinalizationAsync(id);
            return Ok(warnings);
        }

        /// <summary>
        /// Marks board as finalized. Stories can still be reassigned but UI shows 'moved after finalization' badge.
        /// </summary>
        /// <param name="id">Board ID.</param>
        /// <returns>Finalization result with warnings and board summary.</returns>
        /// <response code="200">Board finalized successfully.</response>
        /// <response code="400">Validation failed, cannot finalize.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Board not found.</response>
        [HttpPatch("{id}/finalize")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FinalizeBoard([FromRoute] int id)
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

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, id, "BoardFinalized", payload, initiatorConnectionId);

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

        /// <summary>
        /// Reverts finalization status. Clears 'moved after finalization' flags on stories.
        /// </summary>
        /// <param name="id">Board ID.</param>
        /// <returns>Restoration result with board summary.</returns>
        /// <response code="200">Board restored successfully.</response>
        /// <response code="403">Board is locked.</response>
        /// <response code="404">Board not found.</response>
        [HttpPatch("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RestoreBoard([FromRoute] int id)
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

            string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
            await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, id, "BoardRestored", payload, initiatorConnectionId);

            return Ok(new
            {
                success = true,
                message = "Board restored - editing is now allowed",
                board,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Locks board with password protection. Prevents all mutation operations (403 Forbidden) until unlocked.
        /// </summary>
        /// <param name="id">Board ID.</param>
        /// <param name="dto">Lock request with password.</param>
        /// <returns>Lock success result and board summary.</returns>
        /// <response code="200">Board locked successfully.</response>
        /// <response code="400">Board is already locked.</response>
        /// <response code="401">Invalid password.</response>
        /// <response code="404">Board not found.</response>
        [HttpPatch("{id}/lock")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LockBoard([FromRoute] int id, [FromBody] BoardLockDto dto)
        {
            try
            {
                BoardSummaryDto? board = await _boardService.LockBoardAsync(id, dto.Password);
                if (board == null)
                {
                    return NotFound();
                }

                // Broadcast lock event to all users on this board
                BoardLockStateChangedDto payload = new()
                {
                    BoardId = id,
                    IsLocked = true,
                    TimestampUtc = DateTime.UtcNow
                };

                string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
                await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, id, "BoardLockStateChanged", payload, initiatorConnectionId);

                return Ok(new
                {
                    success = true,
                    message = "Board locked successfully",
                    board,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = ex.Message,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    error = new
                    {
                        message = ex.Message,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
        }

        /// <summary>
        /// Unlocks board with password verification. Restores mutation capabilities.
        /// </summary>
        /// <param name="id">Board ID.</param>
        /// <param name="dto">Unlock request with password.</param>
        /// <returns>Unlock success result and board summary.</returns>
        /// <response code="200">Board unlocked successfully.</response>
        /// <response code="400">Board is not locked.</response>
        /// <response code="401">Invalid password.</response>
        /// <response code="404">Board not found.</response>
        [HttpPatch("{id}/unlock")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnlockBoard([FromRoute] int id, [FromBody] BoardUnlockDto dto)
        {
            try
            {
                BoardSummaryDto? board = await _boardService.UnlockBoardAsync(id, dto.Password);
                if (board == null)
                {
                    return NotFound();
                }

                // Broadcast unlock event to all users on this board
                BoardLockStateChangedDto payload = new()
                {
                    BoardId = id,
                    IsLocked = false,
                    TimestampUtc = DateTime.UtcNow
                };

                string? initiatorConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
                await PlanningHub.BroadcastToBoardAsync(_hubContext.Clients, id, "BoardLockStateChanged", payload, initiatorConnectionId);

                return Ok(new
                {
                    success = true,
                    message = "Board unlocked successfully",
                    board,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = ex.Message,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    error = new
                    {
                        message = ex.Message,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
        }
    }
}
