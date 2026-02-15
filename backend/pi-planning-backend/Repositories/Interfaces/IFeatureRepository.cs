using PiPlanningBackend.Models;

namespace PiPlanningBackend.Repositories.Interfaces
{
    public interface IFeatureRepository
    {
        Task<Feature?> GetByAzureIdAsync(string azureId, int boardId);
        Task<Feature?> GetByIdAsync(int id);
        Task<int> GetMaxPriorityAsync(int boardId);
        Task AddAsync(Feature feature);
        Task SaveChangesAsync();
        Task<List<UserStory>> GetUserStoriesByFeatureAsync(int featureId);
        Task UpdateAsync(Feature feature);
        Task DeleteAsync(Feature feature);
    }
}
