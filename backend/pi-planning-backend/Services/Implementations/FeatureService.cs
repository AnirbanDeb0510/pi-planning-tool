using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Interfaces;
using PiPlanningBackend.Services; // for Azure service interface

namespace PiPlanningBackend.Services.Implementations
{
    public class FeatureService : IFeatureService
    {
        private readonly IFeatureRepository _featureRepo;
        private readonly IUserStoryRepository _storyRepo;
        private readonly IBoardRepository _boardRepo;
        private readonly IAzureBoardsService _azureService;

        public FeatureService(IFeatureRepository featureRepo,
                              IUserStoryRepository storyRepo,
                              IBoardRepository boardRepo,
                              IAzureBoardsService azureService)
        {
            _featureRepo = featureRepo;
            _storyRepo = storyRepo;
            _boardRepo = boardRepo;
            _azureService = azureService;
        }

        // import a feature (from the UI after Fetch from Azure)
        public async Task<FeatureDto> ImportFeatureToBoardAsync(int boardId, FeatureDto featureDto)
        {
            var board = await _boardRepo.GetBoardWithSprintsAsync(boardId);
            if (board == null)
            {
                return new FeatureDto();
            }

            Feature? existing = await CreateOrModifyFeature(boardId, featureDto);

            // insert/update child user stories
            var sprints = board.Sprints.OrderBy(s => s.Id).ToList();

            // For each incoming child story DTO
            var childrenUserStoriesDto = featureDto.Children ?? [];
            await CreateOrUpdateUserStory(existing, sprints, childrenUserStoriesDto);
            if (existing == null)
            {
                return new FeatureDto();
            }

            // return the stored feature dto (re-query to include generated ids)
            var saved = await _featureRepo.GetByIdAsync(existing.Id);
            return new FeatureDto
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
        }

        private async Task CreateOrUpdateUserStory(Feature? existing, List<Sprint> sprints, List<UserStoryDto> childrenUserStoriesDto)
        {
            if (existing == null) return;
            foreach (var userStoryDto in childrenUserStoriesDto)
            {
                var existingStory = await _storyRepo.GetByAzureIdAsync(userStoryDto.AzureId!, existing.Id);
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
                    var newStory = new UserStory
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
                existing = await _featureRepo.GetByAzureIdAsync(featureDto.AzureId!, boardId);

            if (existing != null)
            {
                // update minimal fields
                existing.Title = featureDto.Title;
                existing.Priority = featureDto.Priority;
                existing.ValueArea = featureDto.ValueArea;
                await _featureRepo.UpdateAsync(existing);
            }
            else
            {
                existing = new Feature
                {
                    BoardId = boardId,
                    AzureId = featureDto.AzureId,
                    Title = featureDto.Title,
                    Priority = featureDto.Priority,
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
            var feature = await _featureRepo.GetByIdAsync(featureId);
            if (feature == null || feature.BoardId != boardId) return null;

            var workItem = await _azureService.GetFeatureWithChildrenAsync(organization, project, int.Parse(feature.AzureId!), pat);

            return await ImportFeatureToBoardAsync(boardId, workItem);
        }

        public async Task<UserStoryDto?> RefreshUserStoryFromAzureAsync(int boardId, int storyId, string organization, string project, string pat)
        {
            var story = await _storyRepo.GetByIdAsync(storyId);
            if (story == null) return null;
            if (story.Feature == null)
            {
                // ensure feature loaded
                story = await _featureRepo.GetUserStoriesByFeatureAsync(story.FeatureId).ContinueWith(t => t.Result.FirstOrDefault(u => u.Id == storyId));
            }
            if (story == null) return null;
            var feature = await _featureRepo.GetByIdAsync(story.FeatureId);
            if (feature == null || feature.BoardId != boardId) return null;

            if (string.IsNullOrEmpty(story.AzureId)) throw new Exception("Story has no AzureId");

            var wi = await _azureService.GetUserStoryAsync(organization, project, int.Parse(story.AzureId!), pat);

            story.Title = wi.Title;
            story.StoryPoints = wi.StoryPoints;
            story.DevStoryPoints = wi.DevStoryPoints;
            story.TestStoryPoints = wi.TestStoryPoints;

            await _storyRepo.UpdateAsync(story);
            await _storyRepo.SaveChangesAsync();

            return new UserStoryDto
            {
                Id = story.Id,
                AzureId = story.AzureId,
                Title = story.Title,
                StoryPoints = story.StoryPoints,
                DevStoryPoints = story.DevStoryPoints,
                TestStoryPoints = story.TestStoryPoints
            };
        }

        public async Task MoveUserStoryAsync(int boardId, int storyId, int targetSprintId)
        {
            var story = await _storyRepo.GetByIdAsync(storyId);

            if (story == null || story.Feature == null || story.Feature.BoardId != boardId) return;

            story.SprintId = targetSprintId;
            story.IsMoved = story.OriginalSprintId != story.SprintId;
            await _storyRepo.UpdateAsync(story);
            await _storyRepo.SaveChangesAsync();
        }

        public async Task ReorderFeatureAsync(int boardId, int featureId, int newPriority)
        {
            var feature = await _featureRepo.GetByIdAsync(featureId);
            if (feature == null || feature.BoardId != boardId) return;

            feature.Priority = newPriority;
            await _featureRepo.UpdateAsync(feature);
            await _featureRepo.SaveChangesAsync();
        }

    }
}
