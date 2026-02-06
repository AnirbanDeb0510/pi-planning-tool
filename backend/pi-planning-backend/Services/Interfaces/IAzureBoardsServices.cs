using PiPlanningBackend.DTOs;
using System.Text.Json;

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
        /// <returns></returns>
        public Task<FeatureDto> GetFeatureWithChildrenAsync(string organization, string project, int featureId, string pat);

        /// <summary>
        /// Get a User Story work item from Azure Boards.
        /// </summary>
        /// <param name="organization"></param>
        /// <param name="project"></param>
        /// <param name="userStoryId"></param>
        /// <param name="pat"></param>
        /// <returns></returns>
        public Task<UserStoryDto> GetUserStoryAsync(string organization, string project, int userStoryId, string pat);
    }
}
