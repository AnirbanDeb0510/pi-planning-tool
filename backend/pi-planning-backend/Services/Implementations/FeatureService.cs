using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class FeatureService(IFeatureRepository featureRepo,
                          IUserStoryRepository storyRepo,
                          IBoardRepository boardRepo,
                          IAzureBoardsService azureService,
                          IValidationService validationService,
                          ILogger<FeatureService> logger,
                          ICorrelationIdProvider correlationIdProvider,
                          ITransactionService transactionService) : IFeatureService
    {
        private readonly IFeatureRepository _featureRepo = featureRepo;
        private readonly IUserStoryRepository _storyRepo = storyRepo;
        private readonly IBoardRepository _boardRepo = boardRepo;
        private readonly IAzureBoardsService _azureService = azureService;
        private readonly IValidationService _validationService = validationService;
        private readonly ILogger<FeatureService> _logger = logger;
        private readonly ICorrelationIdProvider _correlationIdProvider = correlationIdProvider;
        private readonly ITransactionService _transactionService = transactionService;

        // import a feature (from the UI after Fetch from Azure)
        public async Task<FeatureDto> ImportFeatureToBoardAsync(int boardId, FeatureDto featureDto, bool checkFinalized = true)
        {
            string? correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Feature import started | CorrelationId: {CorrelationId} | BoardId: {BoardId} | FeatureTitle: {FeatureTitle}",
                correlationId, boardId, featureDto.Title);

            return await _transactionService.ExecuteInTransactionAsync(async () =>
            {
                await _validationService.ValidateBoardExists(boardId);
                Board board = await _boardRepo.GetBoardWithSprintsAsync(boardId) ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

                // Guard: Prevent adding features if board is locked
                _validationService.ValidateBoardNotLocked(board, "add features");

                // Guard: Prevent adding features if board is finalized (unless bypass flag is set for refresh)
                if (checkFinalized)
                {
                    _validationService.ValidateBoardNotFinalized(board, "add features");
                }

                // Create/Modify feature in database
                Feature? existing = await CreateOrModifyFeature(boardId, featureDto);

                // insert/update child user stories
                List<Sprint> sprints = board.Sprints.OrderBy(s => s.Id).ToList();

                // For each incoming child story DTO
                List<UserStoryDto> childrenUserStoriesDto = featureDto.Children ?? [];
                await CreateOrUpdateUserStory(existing, sprints, childrenUserStoriesDto);
                if (existing == null)
                {
                    return new FeatureDto();
                }

                // return the stored feature dto (re-query to include generated ids)
                Feature? saved = await _featureRepo.GetByIdAsync(existing.Id);
                FeatureDto result = new()
                {
                    Id = saved!.Id,
                    AzureId = saved.AzureId,
                    Title = saved.Title,
                    Priority = saved.Priority,
                    ValueArea = saved.ValueArea,
                    Children = [.. saved.UserStories.Select(u => new UserStoryDto
                    {
                        Id = u.Id,
                        AzureId = u.AzureId,
                        Title = u.Title,
                        StoryPoints = u.StoryPoints,
                        DevStoryPoints = u.DevStoryPoints,
                        TestStoryPoints = u.TestStoryPoints
                    })]
                };

                _logger.LogInformation(
                    "Feature imported successfully | CorrelationId: {CorrelationId} | FeatureId: {FeatureId} | Title: {Title} | StoryCount: {StoryCount}",
                    correlationId, saved.Id, saved.Title, saved.UserStories.Count);
                return result;
            });
        }

        private async Task CreateOrUpdateUserStory(Feature? existing, List<Sprint> sprints, List<UserStoryDto> childrenUserStoriesDto)
        {
            if (existing == null)
            {
                return;
            }

            foreach (UserStoryDto userStoryDto in childrenUserStoriesDto)
            {
                UserStory? existingStory = await _storyRepo.GetByAzureIdAsync(userStoryDto.AzureId!, existing.Id);
                if (existingStory != null)
                {
                    // update existing
                    existingStory.Title = userStoryDto.Title;
                    existingStory.StoryPoints = userStoryDto.StoryPoints;
                    existingStory.DevStoryPoints = userStoryDto.DevStoryPoints;
                    existingStory.TestStoryPoints = userStoryDto.TestStoryPoints;
                    await _storyRepo.UpdateAsync(existingStory);
                }
                else
                {
                    UserStory newStory = new()
                    {
                        FeatureId = existing.Id,
                        AzureId = userStoryDto.AzureId,
                        Title = userStoryDto.Title,
                        StoryPoints = userStoryDto.StoryPoints,
                        DevStoryPoints = userStoryDto.DevStoryPoints,
                        TestStoryPoints = userStoryDto.TestStoryPoints,
                        SprintId = sprints[0].Id
                    };
                    await _storyRepo.AddAsync(newStory);
                }
            }

            await _storyRepo.SaveChangesAsync();
        }

        private async Task<Feature?> CreateOrModifyFeature(int boardId, FeatureDto featureDto)
        {
            // if feature with same AzureId exists on board, return existing or update
            Feature? existing = null;
            if (!string.IsNullOrEmpty(featureDto.AzureId))
            {
                existing = await _featureRepo.GetByAzureIdAsync(featureDto.AzureId!, boardId);
            }

            if (existing != null)
            {
                // update minimal fields - preserve priority to maintain user's reordering
                existing.Title = featureDto.Title;
                existing.ValueArea = featureDto.ValueArea;
                // Do NOT update Priority - keep existing order
                await _featureRepo.UpdateAsync(existing);
            }
            else
            {
                // Calculate next priority for new feature
                int maxPriority = await _featureRepo.GetMaxPriorityAsync(boardId);

                existing = new Feature
                {
                    BoardId = boardId,
                    AzureId = featureDto.AzureId,
                    Title = featureDto.Title,
                    Priority = maxPriority + 1,
                    ValueArea = featureDto.ValueArea
                };

                await _featureRepo.AddAsync(existing);
            }

            // save to obtain Id (if new)
            await _featureRepo.SaveChangesAsync();
            return existing;
        }

        // refresh a feature by calling Azure + updating DB records
        public async Task<FeatureDto?> RefreshFeatureFromAzureAsync(int boardId, int featureId, string organization, string project, string pat)
        {
            string? correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Feature refresh from Azure started | CorrelationId: {CorrelationId} | FeatureId: {FeatureId} | BoardId: {BoardId}",
                correlationId, featureId, boardId);

            await _validationService.ValidateFeatureBelongsToBoard(featureId, boardId);
            Feature feature = await _featureRepo.GetByIdAsync(featureId)
                ?? throw new KeyNotFoundException($"Feature with ID {featureId} not found.");
            Board board = await _boardRepo.GetByIdAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            FeatureDto workItem = await _azureService.GetFeatureWithChildrenAsync(
                organization,
                project,
                int.Parse(feature.AzureId!),
                pat,
                board.AzureStoryPointField,
                board.AzureDevStoryPointField,
                board.AzureTestStoryPointField);
            _logger.LogInformation(
                "Feature refreshed from Azure | CorrelationId: {CorrelationId} | FeatureId: {FeatureId} | Title: {Title}",
                correlationId, featureId, workItem.Title);

            // For refresh, bypass finalization check to allow updating on finalized boards
            return await ImportFeatureToBoardAsync(boardId, workItem, checkFinalized: false);
        }

        public async Task<UserStoryDto?> RefreshUserStoryFromAzureAsync(int boardId, int storyId, string organization, string project, string pat)
        {
            string? correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "User story refresh from Azure started | CorrelationId: {CorrelationId} | StoryId: {StoryId} | BoardId: {BoardId}",
                correlationId, storyId, boardId);

            return await _transactionService.ExecuteInTransactionAsync(async () =>
            {
                await _validationService.ValidateStoryBelongsToBoard(storyId, boardId);
                UserStory story = await _storyRepo.GetByIdWithDetailsAsync(storyId)
                    ?? throw new KeyNotFoundException($"User story with ID {storyId} not found.");
                Feature feature = story.Feature
                    ?? throw new KeyNotFoundException($"Feature for story {storyId} not found.");
                Board board = await _boardRepo.GetByIdAsync(boardId)
                    ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

                // Guard: Prevent refresh if board is locked
                _validationService.ValidateBoardNotLocked(board, "refresh user story");

                if (string.IsNullOrEmpty(story.AzureId))
                {
                    throw new Exception("Story has no AzureId");
                }

                UserStoryDto wi = await _azureService.GetUserStoryAsync(
                    organization,
                    project,
                    int.Parse(story.AzureId!),
                    pat,
                    board.AzureStoryPointField,
                    board.AzureDevStoryPointField,
                    board.AzureTestStoryPointField);

                story.Title = wi.Title;
                story.StoryPoints = wi.StoryPoints;
                story.DevStoryPoints = wi.DevStoryPoints;
                story.TestStoryPoints = wi.TestStoryPoints;

                await _storyRepo.UpdateAsync(story);
                await _storyRepo.SaveChangesAsync();

                _logger.LogInformation(
                    "User story refreshed from Azure | CorrelationId: {CorrelationId} | StoryId: {StoryId} | Title: {Title} | Points: {StoryPoints}",
                    correlationId, story.Id, story.Title, story.StoryPoints);

                return new UserStoryDto
                {
                    Id = story.Id,
                    AzureId = story.AzureId,
                    Title = story.Title,
                    StoryPoints = story.StoryPoints,
                    DevStoryPoints = story.DevStoryPoints,
                    TestStoryPoints = story.TestStoryPoints
                };
            });
        }

        public async Task MoveUserStoryAsync(int boardId, int storyId, int targetSprintId)
        {
            string? correlationId = _correlationIdProvider.GetCorrelationId();

            await _validationService.ValidateStoryBelongsToBoard(storyId, boardId);
            await _validationService.ValidateSprintBelongsToBoard(targetSprintId, boardId);
            UserStory story = await _storyRepo.GetByIdWithDetailsAsync(storyId)
                ?? throw new KeyNotFoundException($"User story with ID {storyId} not found.");
            Board board = await _boardRepo.GetByIdAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            // Guard: Prevent moving stories if board is locked
            _validationService.ValidateBoardNotLocked(board, "move user stories");

            int previousSprintId = story.SprintId;
            story.SprintId = targetSprintId;
            story.IsMoved = story.OriginalSprintId != story.SprintId;
            await _storyRepo.UpdateAsync(story);
            await _storyRepo.SaveChangesAsync();

            _logger.LogInformation(
                "User story moved | CorrelationId: {CorrelationId} | StoryId: {StoryId} | PreviousSprint: {PreviousSprint} | TargetSprint: {TargetSprint}",
                correlationId, storyId, previousSprintId, targetSprintId);
        }

        public async Task ReorderFeaturesAsync(int boardId, List<ReorderFeatureItemDto> features)
        {
            string? correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Feature reordering started | CorrelationId: {CorrelationId} | BoardId: {BoardId} | FeatureCount: {FeatureCount}",
                correlationId, boardId, features.Count);

            if (features.Count == 0)
            {
                _logger.LogInformation(
                    "Feature reordering skipped - empty list | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                    correlationId, boardId);
                return;
            }

            await _validationService.ValidateBoardExists(boardId);
            Board board = await _boardRepo.GetByIdAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            // Guard: Prevent reordering if board is locked
            _validationService.ValidateBoardNotLocked(board, "reorder features");

            foreach (ReorderFeatureItemDto item in features)
            {
                await _validationService.ValidateFeatureBelongsToBoard(item.FeatureId, boardId);
                Feature feature = await _featureRepo.GetByIdAsync(item.FeatureId) ?? throw new KeyNotFoundException($"Feature with ID {item.FeatureId} not found.");
                feature.Priority = item.NewPriority;
                await _featureRepo.UpdateAsync(feature);
            }

            await _featureRepo.SaveChangesAsync();

            _logger.LogInformation(
                "Features reordered successfully | CorrelationId: {CorrelationId} | BoardId: {BoardId} | ReorderedCount: {ReorderedCount}",
                correlationId, boardId, features.Count);
        }

        public async Task<bool> DeleteFeatureAsync(int boardId, int featureId)
        {
            string? correlationId = _correlationIdProvider.GetCorrelationId();

            return await _transactionService.ExecuteInTransactionAsync(async () =>
            {
                await _validationService.ValidateFeatureBelongsToBoard(featureId, boardId);
                Feature feature = await _featureRepo.GetByIdAsync(featureId)
                    ?? throw new KeyNotFoundException($"Feature with ID {featureId} not found.");
                Board board = await _boardRepo.GetByIdAsync(boardId)
                    ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

                // Guard: Prevent deletion if board is locked
                _validationService.ValidateBoardNotLocked(board, "delete feature");

                int storyCount = feature.UserStories?.Count ?? 0;
                // Delete feature - EF Core will cascade delete user stories
                await _featureRepo.DeleteAsync(feature);
                await _featureRepo.SaveChangesAsync();

                _logger.LogInformation(
                    "Feature deleted | CorrelationId: {CorrelationId} | FeatureId: {FeatureId} | Title: {Title} | CascadedStories: {StoryCount}",
                    correlationId, featureId, feature.Title, storyCount);
                return true;
            });
        }

    }
}
