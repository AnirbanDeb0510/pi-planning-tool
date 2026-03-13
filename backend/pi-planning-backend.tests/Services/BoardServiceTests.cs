using Microsoft.Extensions.Logging;
using Moq;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Implementations;
using PiPlanningBackend.Services.Interfaces;
using Xunit;

namespace PiPlanningBackend.Tests.Services
{
    public class BoardServiceTests
    {
        private readonly Mock<IBoardRepository> _boardRepository = new();
        private readonly Mock<ISprintService> _sprintService = new();
        private readonly Mock<IValidationService> _validationService = new();
        private readonly Mock<ILogger<BoardService>> _logger = new();
        private readonly Mock<ICorrelationIdProvider> _correlationIdProvider = new();
        private readonly Mock<ITransactionService> _transactionService = new();
        private readonly BoardService _service;

        public BoardServiceTests()
        {
            _correlationIdProvider.Setup(x => x.GetCorrelationId()).Returns("corr-123");

            // Pass the lambda through for Board-returning transactions
            _transactionService
                .Setup(t => t.ExecuteInTransactionAsync(It.IsAny<Func<Task<Board>>>()))
                .Returns<Func<Task<Board>>>(fn => fn());

            // Pass the lambda through for BoardSummaryDto-returning transactions
            _transactionService
                .Setup(t => t.ExecuteInTransactionAsync(It.IsAny<Func<Task<BoardSummaryDto>>>()))
                .Returns<Func<Task<BoardSummaryDto>>>(fn => fn());

            _service = new BoardService(
                _boardRepository.Object,
                _sprintService.Object,
                _validationService.Object,
                _logger.Object,
                _correlationIdProvider.Object,
                _transactionService.Object);
        }

        private static BoardCreateDto CreateDto(string? password = null) => new()
        {
            Name = "PI Board",
            Organization = "Contoso",
            Project = "Alpha",
            NumSprints = 2,
            SprintDuration = 14,
            StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Password = password
        };

        // ── CreateBoardAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task CreateBoardAsync_WithNoPassword_CreatesUnlockedBoardWithSprints()
        {
            BoardCreateDto dto = CreateDto();
            _sprintService
                .Setup(s => s.GenerateSprintsForBoard(It.IsAny<Board>(), 2, 14))
                .Returns([new Sprint { Name = "Sprint 0" }, new Sprint { Name = "Sprint 1" }, new Sprint { Name = "Sprint 2" }]);
            _boardRepository.Setup(r => r.AddAsync(It.IsAny<Board>())).ReturnsAsync((Board b) => b);

            Board board = await _service.CreateBoardAsync(dto);

            Assert.Equal("PI Board", board.Name);
            Assert.Equal("Contoso", board.Organization);
            Assert.False(board.IsLocked);
            Assert.Null(board.PasswordHash);
            Assert.Equal(3, board.Sprints.Count);
        }

        [Fact]
        public async Task CreateBoardAsync_WithPassword_CreatesLockedBoardWithHashedPassword()
        {
            BoardCreateDto dto = CreateDto(password: "secret123");
            _sprintService
                .Setup(s => s.GenerateSprintsForBoard(It.IsAny<Board>(), 2, 14))
                .Returns([]);
            _boardRepository.Setup(r => r.AddAsync(It.IsAny<Board>())).ReturnsAsync((Board b) => b);

            Board board = await _service.CreateBoardAsync(dto);

            Assert.True(board.IsLocked);
            Assert.NotNull(board.PasswordHash);
            Assert.NotEqual("secret123", board.PasswordHash); // must be hashed, not plaintext
        }

        [Fact]
        public async Task CreateBoardAsync_StartDateIsStoredAsUtc()
        {
            BoardCreateDto dto = CreateDto();
            _sprintService
                .Setup(s => s.GenerateSprintsForBoard(It.IsAny<Board>(), 2, 14))
                .Returns([]);
            _boardRepository.Setup(r => r.AddAsync(It.IsAny<Board>())).ReturnsAsync((Board b) => b);

            Board board = await _service.CreateBoardAsync(dto);

            Assert.Equal(DateTimeKind.Utc, board.StartDate.Kind);
        }

        // ── GetBoardAsync ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetBoardAsync_WhenBoardMissing_ThrowsKeyNotFoundException()
        {
            _validationService.Setup(v => v.ValidateBoardExists(99)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Board?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetBoardAsync(99));
        }

        [Fact]
        public async Task GetBoardAsync_WhenBoardFound_ReturnsBoard()
        {
            Board board = new() { Id = 1, Name = "PI Board" };
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);

            Board? result = await _service.GetBoardAsync(1);

            Assert.NotNull(result);
            Assert.Equal("PI Board", result.Name);
        }

        // ── ValidateBoardForFinalizationAsync ─────────────────────────────────────

        [Fact]
        public async Task ValidateBoardForFinalizationAsync_WhenAlreadyFinalized_ReturnsFalseWithMessage()
        {
            Board board = new() { Id = 1, Name = "PI Board", IsFinalized = true };
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithFullHierarchyAsync(1)).ReturnsAsync(board);

            (bool success, List<string> warnings) = await _service.ValidateBoardForFinalizationAsync(1);

            Assert.False(success);
            Assert.Contains("Board is already finalized", warnings);
        }

        [Fact]
        public async Task ValidateBoardForFinalizationAsync_WithEmptyBoard_ReturnsTrueWithWarnings()
        {
            // Board with no team members, no features, and only Sprint 0 (count == 1)
            Board board = new() { Id = 1, Name = "PI Board", IsFinalized = false };
            board.Sprints.Add(new Sprint { Name = "Sprint 0" }); // only 1 sprint triggers the warning
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithFullHierarchyAsync(1)).ReturnsAsync(board);

            (bool success, List<string> warnings) = await _service.ValidateBoardForFinalizationAsync(1);

            Assert.True(success);
            Assert.Contains(warnings, w => w.Contains("No team members"));
            Assert.Contains(warnings, w => w.Contains("No features"));
            Assert.Contains(warnings, w => w.Contains("No planned sprints"));
        }

        [Fact]
        public async Task ValidateBoardForFinalizationAsync_WhenBoardReadyForFinalization_ReturnsTrueWithNoWarnings()
        {
            UserStory story = new() { Id = 1, Title = "Story 1", SprintId = 2 };
            Feature feature = new() { Title = "Feature 1", UserStories = [story] };
            TeamMember member = new() { Id = 1, Name = "Alice", TeamMemberSprints = [new TeamMemberSprint()] };
            Board board = new()
            {
                Id = 1,
                Name = "PI Board",
                IsFinalized = false,
                Features = [feature],
                TeamMembers = [member],
                Sprints = [new Sprint { Name = "Sprint 0" }, new Sprint { Name = "Sprint 1" }]
            };
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithFullHierarchyAsync(1)).ReturnsAsync(board);

            (bool success, List<string> warnings) = await _service.ValidateBoardForFinalizationAsync(1);

            Assert.True(success);
            Assert.Empty(warnings);
        }

        // ── FinalizeBoardAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task FinalizeBoardAsync_WhenBoardLocked_ThrowsUnauthorizedAccessException()
        {
            Board board = new() { Id = 1, Name = "PI Board", IsLocked = true };
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithFullHierarchyAsync(1)).ReturnsAsync(board);
            _validationService
                .Setup(v => v.ValidateBoardNotLocked(board, It.IsAny<string>()))
                .Throws<UnauthorizedAccessException>();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.FinalizeBoardAsync(1));
        }

        [Fact]
        public async Task FinalizeBoardAsync_SetsIsFinalizedAndOriginalSprintIdsOnAllStories()
        {
            UserStory story1 = new() { Id = 10, Title = "S1", SprintId = 2 };
            UserStory story2 = new() { Id = 11, Title = "S2", SprintId = 3 };
            Feature feature = new() { Title = "F1", UserStories = [story1, story2] };
            Board board = new() { Id = 1, Name = "PI Board", Features = [feature] };
            Board boardForPreview = new() { Id = 1, Name = "PI Board" };

            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithFullHierarchyAsync(1)).ReturnsAsync(board);
            _boardRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithFeaturesAsync(1)).ReturnsAsync(boardForPreview);

            await _service.FinalizeBoardAsync(1);

            Assert.True(board.IsFinalized);
            Assert.NotNull(board.FinalizedAt);
            Assert.Equal(2, story1.OriginalSprintId);
            Assert.Equal(3, story2.OriginalSprintId);
        }
    }
}
