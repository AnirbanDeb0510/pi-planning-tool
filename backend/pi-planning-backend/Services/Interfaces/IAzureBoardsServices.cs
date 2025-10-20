using PiPlanningBackend.DTOs;
using System.Text.Json;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface IAzureBoardsService
    {
        /// <summary>
        /// Fetches a single Azure DevOps work item (Feature or Story) with its relations expanded.
        /// </summary>
        Task<JsonElement?> GetWorkItemWithRelationsAsync(string organization, string project, int id, string pat);

        /// <summary>
        /// Fetches multiple work items by their IDs.
        /// </summary>
        Task<JsonElement?> GetWorkItemsAsync(string organization, string project, IEnumerable<int> ids, string pat);

        /// <summary>
        /// Converts a raw Azure work item JSON to a Feature DTO.
        /// </summary>
        FeatureDto ParseFeature(JsonElement workItem);

        /// <summary>
        /// Converts a raw Azure work item JSON to a User Story DTO, 
        /// dynamically mapping Story Point fields.
        /// </summary>
        UserStoryDto ParseUserStory(
            JsonElement workItem,
            string storyPointField = "Microsoft.VSTS.Scheduling.StoryPoints",
            string devField = "Custom.DevStoryPoints",
            string testField = "Custom.TestStoryPoints"
        );
    }
}
