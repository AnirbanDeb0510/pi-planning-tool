using PiPlanningBackend.DTOs;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface IAzureBoardsService
    {
        /// <summary>
        /// Get a Feature work item along with its child User Stories from Azure Boards.
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="featureId"></param>
        /// <param name="pat"></param>
        /// <param name="storyPointField">Optional Azure field name for story points.</param>
        /// <param name="devField">Optional Azure field name for dev points.</param>
        /// <param name="testField">Optional Azure field name for test points.</param>
        /// <returns></returns>
        Task<FeatureDto> GetFeatureWithChildrenAsync(
            string organization,
            string project,
            int featureId,
            string pat,
            string? storyPointField = null,
            string? devField = null,
            string? testField = null);

        /// <summary>
        /// Get a Feature work item and children using board-level Azure configuration.
        /// </summary>
        /// <param name="boardId"></param>
        /// <param name="featureId"></param>
        /// <param name="pat"></param>
        /// <returns></returns>
        Task<FeatureDto> GetFeatureWithChildrenForBoardAsync(int boardId, int featureId, string pat);

        /// <summary>
        /// Get a User Story work item from Azure Boards.
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="userStoryId"></param>
        /// <param name="pat"></param>
        /// <param name="storyPointField">Optional Azure field name for story points.</param>
        /// <param name="devField">Optional Azure field name for dev points.</param>
        /// <param name="testField">Optional Azure field name for test points.</param>
        /// <returns></returns>
        Task<UserStoryDto> GetUserStoryAsync(
            string organization,
            string project,
            int userStoryId,
            string pat,
            string? storyPointField = null,
            string? devField = null,
            string? testField = null);
    }
}
