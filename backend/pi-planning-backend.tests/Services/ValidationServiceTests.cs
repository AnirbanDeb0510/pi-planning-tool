using Moq;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Implementations;
using Xunit;

namespace PiPlanningBackend.Tests.Services
{
    public class ValidationServiceTests
    {
        private readonly Mock<IBoardRepository> _boardRepository = new();
        private readonly Mock<IFeatureRepository> _featureRepository = new();
        private readonly Mock<IUserStoryRepository> _userStoryRepository = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly ValidationService _service;

        public ValidationServiceTests()
        {
            _service = new ValidationService(
                _boardRepository.Object,
                _featureRepository.Object,
                _userStoryRepository.Object,
                _teamRepository.Object);
        }

        // ── ValidateBoardNotLocked ────────────────────────────────────────────

        [Fact]
        public void ValidateBoardNotLocked_WhenLocked_ThrowsUnauthorizedAccessException()
        {
            Board board = new() { Name = "PI Board", IsLocked = true };

            Assert.Throws<UnauthorizedAccessException>(() => _service.ValidateBoardNotLocked(board, "update board"));
        }

        [Fact]
        public void ValidateBoardNotLocked_WhenUnlocked_DoesNotThrow()
        {
            Board board = new() { Name = "PI Board", IsLocked = false };

            Exception? exception = Record.Exception(() => _service.ValidateBoardNotLocked(board, "update board"));

            Assert.Null(exception);
        }

        // ── ValidateBoardNotFinalized ─────────────────────────────────────────

        [Fact]
        public void ValidateBoardNotFinalized_WhenFinalized_ThrowsInvalidOperationException()
        {
            Board board = new() { Name = "PI Board", IsFinalized = true };

            Assert.Throws<InvalidOperationException>(() => _service.ValidateBoardNotFinalized(board, "update board"));
        }

        [Fact]
        public void ValidateBoardNotFinalized_WhenNotFinalized_DoesNotThrow()
        {
            Board board = new() { Name = "PI Board", IsFinalized = false };

            Exception? exception = Record.Exception(() => _service.ValidateBoardNotFinalized(board, "update board"));

            Assert.Null(exception);
        }

        // ── ValidateBoardExists ───────────────────────────────────────────────

        [Fact]
        public async Task ValidateBoardExists_WhenBoardMissing_ThrowsKeyNotFoundException()
        {
            _boardRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Board?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateBoardExists(99));
        }

        [Fact]
        public async Task ValidateBoardExists_WhenBoardFound_DoesNotThrow()
        {
            _boardRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Board { Id = 1, Name = "PI Board" });

            Exception? exception = await Record.ExceptionAsync(() => _service.ValidateBoardExists(1));

            Assert.Null(exception);
        }

        // ── ValidateTeamMemberCapacity ────────────────────────────────────────

        [Fact]
        public void ValidateTeamMemberCapacity_WhenNegative_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.ValidateTeamMemberCapacity(-1, 10));
        }

        [Fact]
        public void ValidateTeamMemberCapacity_WhenExceedsSprintDays_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.ValidateTeamMemberCapacity(11, 10));
        }

        [Fact]
        public void ValidateTeamMemberCapacity_WhenWithinRange_DoesNotThrow()
        {
            Exception? exception = Record.Exception(() => _service.ValidateTeamMemberCapacity(8, 10));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTeamMemberCapacity_WhenZero_DoesNotThrow()
        {
            Exception? exception = Record.Exception(() => _service.ValidateTeamMemberCapacity(0, 10));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTeamMemberCapacity_WhenEqualToSprintDays_DoesNotThrow()
        {
            Exception? exception = Record.Exception(() => _service.ValidateTeamMemberCapacity(10, 10));

            Assert.Null(exception);
        }

        // ── ValidateStoryBelongsToBoard ───────────────────────────────────────

        [Fact]
        public async Task ValidateStoryBelongsToBoard_WhenStoryMissing_ThrowsKeyNotFoundException()
        {
            _userStoryRepository.Setup(r => r.GetByIdWithDetailsAsync(42)).ReturnsAsync((UserStory?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateStoryBelongsToBoard(42, 1));
        }

        [Fact]
        public async Task ValidateStoryBelongsToBoard_WhenStoryFromDifferentBoard_ThrowsKeyNotFoundException()
        {
            UserStory story = new()
            {
                Id = 42,
                Title = "Story",
                SprintId = 1,
                Feature = new Feature { BoardId = 99, Title = "Feature" }
            };
            _userStoryRepository.Setup(r => r.GetByIdWithDetailsAsync(42)).ReturnsAsync(story);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateStoryBelongsToBoard(42, 1));
        }

        [Fact]
        public async Task ValidateStoryBelongsToBoard_WhenStoryOnBoard_DoesNotThrow()
        {
            UserStory story = new()
            {
                Id = 42,
                Title = "Story",
                SprintId = 1,
                Feature = new Feature { BoardId = 1, Title = "Feature" }
            };
            _userStoryRepository.Setup(r => r.GetByIdWithDetailsAsync(42)).ReturnsAsync(story);

            Exception? exception = await Record.ExceptionAsync(() => _service.ValidateStoryBelongsToBoard(42, 1));

            Assert.Null(exception);
        }

        // ── ValidateTeamMemberBelongsToBoard ──────────────────────────────────

        [Fact]
        public async Task ValidateTeamMemberBelongsToBoard_WhenMemberMissing_ThrowsKeyNotFoundException()
        {
            _teamRepository.Setup(r => r.GetTeamMemberAsync(5)).ReturnsAsync((TeamMember?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateTeamMemberBelongsToBoard(5, 1));
        }

        [Fact]
        public async Task ValidateTeamMemberBelongsToBoard_WhenMemberOnDifferentBoard_ThrowsKeyNotFoundException()
        {
            TeamMember member = new() { Id = 5, BoardId = 99, Name = "Alice" };
            _teamRepository.Setup(r => r.GetTeamMemberAsync(5)).ReturnsAsync(member);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateTeamMemberBelongsToBoard(5, 1));
        }

        [Fact]
        public async Task ValidateTeamMemberBelongsToBoard_WhenMemberOnBoard_DoesNotThrow()
        {
            TeamMember member = new() { Id = 5, BoardId = 1, Name = "Alice" };
            _teamRepository.Setup(r => r.GetTeamMemberAsync(5)).ReturnsAsync(member);

            Exception? exception = await Record.ExceptionAsync(() => _service.ValidateTeamMemberBelongsToBoard(5, 1));

            Assert.Null(exception);
        }

        // ── ValidateFeatureBelongsToBoard ─────────────────────────────────────

        [Fact]
        public async Task ValidateFeatureBelongsToBoard_WhenFeatureMissing_ThrowsKeyNotFoundException()
        {
            _featureRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Feature?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateFeatureBelongsToBoard(10, 1));
        }

        [Fact]
        public async Task ValidateFeatureBelongsToBoard_WhenFeatureOnDifferentBoard_ThrowsKeyNotFoundException()
        {
            Feature feature = new() { Id = 10, BoardId = 99, Title = "Feature" };
            _featureRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(feature);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateFeatureBelongsToBoard(10, 1));
        }

        [Fact]
        public async Task ValidateFeatureBelongsToBoard_WhenFeatureOnBoard_DoesNotThrow()
        {
            Feature feature = new() { Id = 10, BoardId = 1, Title = "Feature" };
            _featureRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(feature);

            Exception? exception = await Record.ExceptionAsync(() => _service.ValidateFeatureBelongsToBoard(10, 1));

            Assert.Null(exception);
        }

        // ── ValidateSprintBelongsToBoard ──────────────────────────────────────

        [Fact]
        public async Task ValidateSprintBelongsToBoard_WhenBoardMissing_ThrowsKeyNotFoundException()
        {
            _boardRepository.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync((Board?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateSprintBelongsToBoard(7, 1));
        }

        [Fact]
        public async Task ValidateSprintBelongsToBoard_WhenSprintNotInBoard_ThrowsKeyNotFoundException()
        {
            Board board = new() { Id = 1, Name = "PI Board", Sprints = [new Sprint { Id = 3 }] };
            _boardRepository.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ValidateSprintBelongsToBoard(7, 1));
        }

        [Fact]
        public async Task ValidateSprintBelongsToBoard_WhenSprintInBoard_DoesNotThrow()
        {
            Board board = new() { Id = 1, Name = "PI Board", Sprints = [new Sprint { Id = 7 }] };
            _boardRepository.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);

            Exception? exception = await Record.ExceptionAsync(() => _service.ValidateSprintBelongsToBoard(7, 1));

            Assert.Null(exception);
        }
    }
}
