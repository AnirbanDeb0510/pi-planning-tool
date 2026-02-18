using PiPlanningBackend.DTOs;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface IFeatureService
    {
        Task<FeatureDto> ImportFeatureToBoardAsync(int boardId, FeatureDto featureDto, bool checkFinalized = true);
        Task<FeatureDto?> RefreshFeatureFromAzureAsync(int boardId, int featureId, string organization, string project, string pat);
        Task<UserStoryDto?> RefreshUserStoryFromAzureAsync(int boardId, int storyId, string organization, string project, string pat);
        Task MoveUserStoryAsync(int boardId, int storyId, int targetSprintId);
        Task ReorderFeaturesAsync(int boardId, List<ReorderFeatureItemDto> features);
        Task<bool> DeleteFeatureAsync(int boardId, int featureId);
    }
}
