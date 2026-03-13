using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using PiPlanningBackend.Controllers;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Interfaces;
using Xunit;

namespace PiPlanningBackend.Tests.Controllers
{
    public class BoardsControllerTests
    {
        private readonly Mock<IBoardService> _boardService = new();
        private readonly Mock<IHubContext<PlanningHub>> _hubContext = new();
        private readonly BoardsController _controller;

        public BoardsControllerTests()
        {
            _controller = new BoardsController(_boardService.Object, _hubContext.Object);
        }

        [Fact]
        public async Task CreateBoard_ReturnsCreatedAtActionWithMappedResponse()
        {
            BoardCreateDto request = new()
            {
                Name = "PI Board",
                Organization = "Contoso",
                Project = "Project A",
                NumSprints = 4,
                SprintDuration = 14,
                StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            Board createdBoard = new()
            {
                Id = 10,
                Name = request.Name,
                Organization = request.Organization,
                Project = request.Project,
                NumSprints = request.NumSprints,
                SprintDuration = request.SprintDuration,
                StartDate = request.StartDate,
                IsLocked = false,
                IsFinalized = false,
                DevTestToggle = false,
                CreatedAt = DateTime.UtcNow
            };
            _boardService.Setup(s => s.CreateBoardAsync(request)).ReturnsAsync(createdBoard);

            IActionResult result = await _controller.CreateBoard(request);

            CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(BoardsController.GetBoard), created.ActionName);
            Assert.Equal(10, created.RouteValues!["id"]);

            BoardCreatedDto payload = Assert.IsType<BoardCreatedDto>(created.Value);
            Assert.Equal("PI Board", payload.Name);
            Assert.Equal(4, payload.NumSprints);
            Assert.Equal(14, payload.SprintDuration);
        }

        [Fact]
        public async Task GetBoard_WhenServiceReturnsNull_ReturnsNotFound()
        {
            _boardService.Setup(s => s.GetBoardWithHierarchyAsync(99)).ReturnsAsync((BoardResponseDto?)null);

            IActionResult result = await _controller.GetBoard(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetBoard_WhenServiceReturnsBoard_ReturnsOk()
        {
            BoardResponseDto board = new() { Id = 1, Name = "PI Board", Sprints = [] };
            _boardService.Setup(s => s.GetBoardWithHierarchyAsync(1)).ReturnsAsync(board);

            IActionResult result = await _controller.GetBoard(1);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            BoardResponseDto payload = Assert.IsType<BoardResponseDto>(ok.Value);
            Assert.Equal(1, payload.Id);
            Assert.Equal("PI Board", payload.Name);
        }

        [Fact]
        public async Task SearchBoards_TrimsOrganizationAndProject_BeforeCallingService()
        {
            _boardService.Setup(s => s.SearchBoardsAsync("query", "Contoso", "Project A", true, false))
                .ReturnsAsync(new List<BoardSummaryDto>());

            IActionResult result = await _controller.SearchBoards("query", "  Contoso  ", "  Project A  ", true, false);

            Assert.IsType<OkObjectResult>(result);
            _boardService.Verify(
                s => s.SearchBoardsAsync("query", "Contoso", "Project A", true, false),
                Times.Once);
        }

        [Fact]
        public async Task FinalizeBoard_WhenValidationFails_ReturnsBadRequest()
        {
            _boardService
                .Setup(s => s.ValidateBoardForFinalizationAsync(1))
                .ReturnsAsync((false, ["Board is already finalized"]));

            IActionResult result = await _controller.FinalizeBoard(1);

            BadRequestObjectResult bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task FinalizeBoard_WhenFinalizeReturnsNull_ReturnsNotFound()
        {
            _boardService
                .Setup(s => s.ValidateBoardForFinalizationAsync(1))
                .ReturnsAsync((true, new List<string>()));
            _boardService
                .Setup(s => s.FinalizeBoardAsync(1))
                .ReturnsAsync((BoardSummaryDto?)null);

            IActionResult result = await _controller.FinalizeBoard(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task LockBoard_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
        {
            _boardService
                .Setup(s => s.LockBoardAsync(1, "pwd"))
                .ThrowsAsync(new InvalidOperationException("Board is already locked"));

            IActionResult result = await _controller.LockBoard(1, new BoardLockDto { Password = "pwd" });

            BadRequestObjectResult bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UnlockBoard_WhenServiceThrowsUnauthorized_ReturnsUnauthorized()
        {
            _boardService
                .Setup(s => s.UnlockBoardAsync(1, "wrong"))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid password"));

            IActionResult result = await _controller.UnlockBoard(1, new BoardUnlockDto { Password = "wrong" });

            UnauthorizedObjectResult unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorized.Value);
        }
    }
}
