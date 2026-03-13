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
    public class FeatureServiceTests
    {
        private readonly Mock<IFeatureRepository> _featureRepo = new();
        private readonly Mock<IUserStoryRepository> _storyRepo = new();
        private readonly Mock<IBoardRepository> _boardRepo = new();
        private readonly Mock<IAzureBoardsService> _azureService = new();
        private readonly Mock<IValidationService> _validationService = new();
        private readonly Mock<ILogger<FeatureService>> _logger = new();
        private readonly Mock<ICorrelationIdProvider> _correlationIdProvider = new();
        private readonly Mock<ITransactionService> _transactionService = new();
        private readonly FeatureService _service;

        public FeatureServiceTests()
        {
            _correlationIdProvider.Setup(x => x.GetCorrelationId()).Returns("corr-123");

            _transactionService
                .Setup(t => t.ExecuteInTransactionAsync(It.IsAny<Func<Task<FeatureDto>>>()))
                .Returns<Func<Task<FeatureDto>>>(fn => fn());

            _transactionService
                .Setup(t => t.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(fn => fn());

            _transactionService
                .Setup(t => t.ExecuteInTransactionAsync(It.IsAny<Func<Task<UserStoryDto>>>()))
                .Returns<Func<Task<UserStoryDto>>>(fn => fn());

            _service = new FeatureService(
                _featureRepo.Object,
                _storyRepo.Object,
                _boardRepo.Object,
                _azureService.Object,
                _validationService.Object,
                _logger.Object,
                _correlationIdProvider.Object,
                _transactionService.Object);
        }

        private static Board CreateBoard(bool isLocked = false, bool isFinalized = false) => new()
        {
            Id = 1,
            Name = "PI Board",
            IsLocked = isLocked,
            IsFinalized = isFinalized,
            Sprints = [new Sprint { Id = 1, Name = "Sprint 0" }, new Sprint { Id = 2, Name = "Sprint 1" }]
        };

        private static FeatureDto CreateFeatureDto(string? azureId = "azure-1") => new()
        {
            Title = "Feature Alpha",
            AzureId = azureId,
            ValueArea = "Business",
            Children = []
        };

        // ── ImportFeatureToBoardAsync ──────────────────────────────────────────────

        [Fact]
        public async Task ImportFeatureToBoardAsync_WhenBoardLocked_ThrowsUnauthorizedAccessException()
        {
            Board board = CreateBoard(isLocked: true);
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _validationService
                .Setup(v => v.ValidateBoardNotLocked(board, It.IsAny<string>()))
                .Throws<UnauthorizedAccessException>();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.ImportFeatureToBoardAsync(1, CreateFeatureDto()));
        }

        [Fact]
        public async Task ImportFeatureToBoardAsync_WhenBoardFinalized_ThrowsInvalidOperationException()
        {
            Board board = CreateBoard(isFinalized: true);
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _validationService
                .Setup(v => v.ValidateBoardNotFinalized(board, It.IsAny<string>()))
                .Throws<InvalidOperationException>();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ImportFeatureToBoardAsync(1, CreateFeatureDto()));
        }

        [Fact]
        public async Task ImportFeatureToBoardAsync_WhenFinalizedButCheckSkipped_DoesNotThrow()
        {
            Board board = CreateBoard(isFinalized: true);
            Feature savedFeature = new() { Id = 5, Title = "Feature Alpha", AzureId = "azure-1", UserStories = [] };

            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            // ValidateBoardNotFinalized is NOT called — no setup needed
            _featureRepo.Setup(r => r.GetByAzureIdAsync("azure-1", 1)).ReturnsAsync((Feature?)null);
            _featureRepo.Setup(r => r.GetMaxPriorityAsync(1)).ReturnsAsync(0);
            _featureRepo.Setup(r => r.AddAsync(It.IsAny<Feature>())).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(savedFeature);
            _storyRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // checkFinalized: false bypasses the finalization guard
            Exception? ex = await Record.ExceptionAsync(
                () => _service.ImportFeatureToBoardAsync(1, CreateFeatureDto(), checkFinalized: false));

            Assert.Null(ex);
        }

        [Fact]
        public async Task ImportFeatureToBoardAsync_NewFeature_AssignsPriorityAsMaxPlusOne()
        {
            Board board = CreateBoard();
            Feature savedFeature = new() { Id = 5, Title = "Feature Alpha", AzureId = "azure-1", Priority = 3, UserStories = [] };

            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _featureRepo.Setup(r => r.GetByAzureIdAsync("azure-1", 1)).ReturnsAsync((Feature?)null);
            _featureRepo.Setup(r => r.GetMaxPriorityAsync(1)).ReturnsAsync(2); // max is 2 → new gets 3
            _featureRepo.Setup(r => r.AddAsync(It.IsAny<Feature>())).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(savedFeature);
            _storyRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            FeatureDto result = await _service.ImportFeatureToBoardAsync(1, CreateFeatureDto());

            Assert.Equal(3, result.Priority);
            _featureRepo.Verify(r => r.AddAsync(It.Is<Feature>(f => f.Priority == 3)), Times.Once);
        }

        [Fact]
        public async Task ImportFeatureToBoardAsync_ExistingFeature_UpdatesTitleAndValueAreaButNotPriority()
        {
            Board board = CreateBoard();
            Feature existingFeature = new() { Id = 5, Title = "Old Title", AzureId = "azure-1", Priority = 10, ValueArea = "Old" };
            Feature savedFeature = new() { Id = 5, Title = "Feature Alpha", AzureId = "azure-1", Priority = 10, ValueArea = "Business", UserStories = [] };

            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _featureRepo.Setup(r => r.GetByAzureIdAsync("azure-1", 1)).ReturnsAsync(existingFeature);
            _featureRepo.Setup(r => r.UpdateAsync(existingFeature)).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(savedFeature);
            _storyRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            FeatureDto result = await _service.ImportFeatureToBoardAsync(1, CreateFeatureDto());

            // Title and ValueArea updated
            Assert.Equal("Feature Alpha", existingFeature.Title);
            Assert.Equal("Business", existingFeature.ValueArea);
            // Priority NOT changed
            Assert.Equal(10, existingFeature.Priority);
            // AddAsync was never called — it was an update
            _featureRepo.Verify(r => r.AddAsync(It.IsAny<Feature>()), Times.Never);
        }

        [Fact]
        public async Task ImportFeatureToBoardAsync_NewChildStory_AddedWithSprintZeroId()
        {
            Board board = CreateBoard(); // Sprints: Id=1 (Sprint 0), Id=2 (Sprint 1)
            Feature savedFeature = new()
            {
                Id = 5,
                Title = "Feature Alpha",
                AzureId = "azure-1",
                UserStories = [new UserStory { Id = 10, AzureId = "story-1", Title = "Story 1" }]
            };
            FeatureDto featureDto = new()
            {
                Title = "Feature Alpha",
                AzureId = "azure-1",
                Children = [new UserStoryDto { AzureId = "story-1", Title = "Story 1", StoryPoints = 3 }]
            };

            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _featureRepo.Setup(r => r.GetByAzureIdAsync("azure-1", 1)).ReturnsAsync((Feature?)null);
            _featureRepo.Setup(r => r.GetMaxPriorityAsync(1)).ReturnsAsync(0);
            _featureRepo.Setup(r => r.AddAsync(It.IsAny<Feature>())).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(savedFeature);
            _storyRepo.Setup(r => r.GetByAzureIdAsync("story-1", It.IsAny<int>())).ReturnsAsync((UserStory?)null);
            _storyRepo.Setup(r => r.AddAsync(It.IsAny<UserStory>())).Returns(Task.CompletedTask);
            _storyRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.ImportFeatureToBoardAsync(1, featureDto);

            // New story must be assigned to Sprint 0 (the first sprint, Id = 1)
            _storyRepo.Verify(r => r.AddAsync(It.Is<UserStory>(s => s.SprintId == 1)), Times.Once);
        }

        [Fact]
        public async Task ImportFeatureToBoardAsync_ExistingChildStory_UpdatesFieldsInsteadOfAdding()
        {
            Board board = CreateBoard();
            Feature savedFeature = new() { Id = 5, Title = "Feature Alpha", AzureId = "azure-1", UserStories = [] };
            UserStory existingStory = new() { Id = 10, AzureId = "story-1", Title = "Old Title", StoryPoints = 1, SprintId = 1 };
            FeatureDto featureDto = new()
            {
                Title = "Feature Alpha",
                AzureId = "azure-1",
                Children = [new UserStoryDto { AzureId = "story-1", Title = "New Title", StoryPoints = 5 }]
            };

            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _featureRepo.Setup(r => r.GetByAzureIdAsync("azure-1", 1)).ReturnsAsync((Feature?)null);
            _featureRepo.Setup(r => r.GetMaxPriorityAsync(1)).ReturnsAsync(0);
            _featureRepo.Setup(r => r.AddAsync(It.IsAny<Feature>())).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(savedFeature);
            _storyRepo.Setup(r => r.GetByAzureIdAsync("story-1", It.IsAny<int>())).ReturnsAsync(existingStory);
            _storyRepo.Setup(r => r.UpdateAsync(existingStory)).Returns(Task.CompletedTask);
            _storyRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.ImportFeatureToBoardAsync(1, featureDto);

            Assert.Equal("New Title", existingStory.Title);
            Assert.Equal(5, existingStory.StoryPoints);
            _storyRepo.Verify(r => r.AddAsync(It.IsAny<UserStory>()), Times.Never);
            _storyRepo.Verify(r => r.UpdateAsync(existingStory), Times.Once);
        }

        // ── MoveUserStoryAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task MoveUserStoryAsync_MovesToDifferentSprint_SetsIsMovedTrue()
        {
            UserStory story = new() { Id = 10, SprintId = 1, OriginalSprintId = 1, Feature = new Feature { BoardId = 1, Title = "F" } };
            Board board = new() { Id = 1, Name = "PI Board" };

            _validationService.Setup(v => v.ValidateStoryBelongsToBoard(10, 1)).Returns(Task.CompletedTask);
            _validationService.Setup(v => v.ValidateSprintBelongsToBoard(2, 1)).Returns(Task.CompletedTask);
            _storyRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(story);
            _boardRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _storyRepo.Setup(r => r.UpdateAsync(story)).Returns(Task.CompletedTask);
            _storyRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.MoveUserStoryAsync(1, 10, 2);

            Assert.Equal(2, story.SprintId);
            Assert.True(story.IsMoved);
        }

        [Fact]
        public async Task MoveUserStoryAsync_MovedBackToOriginalSprint_SetsIsMovedFalse()
        {
            UserStory story = new() { Id = 10, SprintId = 2, OriginalSprintId = 1, Feature = new Feature { BoardId = 1, Title = "F" } };
            Board board = new() { Id = 1, Name = "PI Board" };

            _validationService.Setup(v => v.ValidateStoryBelongsToBoard(10, 1)).Returns(Task.CompletedTask);
            _validationService.Setup(v => v.ValidateSprintBelongsToBoard(1, 1)).Returns(Task.CompletedTask);
            _storyRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(story);
            _boardRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _storyRepo.Setup(r => r.UpdateAsync(story)).Returns(Task.CompletedTask);
            _storyRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Move back to originalSprintId = 1
            await _service.MoveUserStoryAsync(1, 10, 1);

            Assert.Equal(1, story.SprintId);
            Assert.False(story.IsMoved);
        }

        [Fact]
        public async Task MoveUserStoryAsync_WhenBoardLocked_ThrowsUnauthorizedAccessException()
        {
            UserStory story = new() { Id = 10, SprintId = 1, Feature = new Feature { BoardId = 1, Title = "F" } };
            Board board = new() { Id = 1, Name = "PI Board", IsLocked = true };

            _validationService.Setup(v => v.ValidateStoryBelongsToBoard(10, 1)).Returns(Task.CompletedTask);
            _validationService.Setup(v => v.ValidateSprintBelongsToBoard(2, 1)).Returns(Task.CompletedTask);
            _storyRepo.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(story);
            _boardRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _validationService
                .Setup(v => v.ValidateBoardNotLocked(board, It.IsAny<string>()))
                .Throws<UnauthorizedAccessException>();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.MoveUserStoryAsync(1, 10, 2));
        }

        // ── ReorderFeaturesAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task ReorderFeaturesAsync_WithEmptyList_DoesNotCallRepository()
        {
            await _service.ReorderFeaturesAsync(1, []);

            _boardRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _featureRepo.Verify(r => r.UpdateAsync(It.IsAny<Feature>()), Times.Never);
        }

        [Fact]
        public async Task ReorderFeaturesAsync_UpdatesPriorityForEachFeature()
        {
            Board board = new() { Id = 1, Name = "PI Board" };
            Feature f1 = new() { Id = 10, Title = "F1", Priority = 1, BoardId = 1 };
            Feature f2 = new() { Id = 20, Title = "F2", Priority = 2, BoardId = 1 };
            List<ReorderFeatureItemDto> items =
            [
                new() { FeatureId = 10, NewPriority = 2 },
                new() { FeatureId = 20, NewPriority = 1 }
            ];

            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _validationService.Setup(v => v.ValidateFeatureBelongsToBoard(10, 1)).Returns(Task.CompletedTask);
            _validationService.Setup(v => v.ValidateFeatureBelongsToBoard(20, 1)).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(f1);
            _featureRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(f2);
            _featureRepo.Setup(r => r.UpdateAsync(It.IsAny<Feature>())).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.ReorderFeaturesAsync(1, items);

            Assert.Equal(2, f1.Priority);
            Assert.Equal(1, f2.Priority);
        }

        // ── DeleteFeatureAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteFeatureAsync_HappyPath_ReturnsTrue()
        {
            Feature feature = new() { Id = 10, Title = "F1", BoardId = 1 };
            Board board = new() { Id = 1, Name = "PI Board" };

            _validationService.Setup(v => v.ValidateFeatureBelongsToBoard(10, 1)).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(feature);
            _boardRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _featureRepo.Setup(r => r.DeleteAsync(feature)).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            bool result = await _service.DeleteFeatureAsync(1, 10);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFeatureAsync_WhenBoardLocked_ThrowsUnauthorizedAccessException()
        {
            Feature feature = new() { Id = 10, Title = "F1", BoardId = 1 };
            Board board = new() { Id = 1, Name = "PI Board", IsLocked = true };

            _validationService.Setup(v => v.ValidateFeatureBelongsToBoard(10, 1)).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(feature);
            _boardRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _validationService
                .Setup(v => v.ValidateBoardNotLocked(board, It.IsAny<string>()))
                .Throws<UnauthorizedAccessException>();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.DeleteFeatureAsync(1, 10));
        }

        // ── RefreshFeatureFromAzureAsync ──────────────────────────────────────────

        [Fact]
        public async Task RefreshFeatureFromAzureAsync_WhenFeatureNotFound_ThrowsKeyNotFoundException()
        {
            _validationService.Setup(v => v.ValidateFeatureBelongsToBoard(99, 1)).Returns(Task.CompletedTask);
            _featureRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Feature?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.RefreshFeatureFromAzureAsync(1, 99, "Contoso", "Alpha", "pat-token"));
        }
    }
}
