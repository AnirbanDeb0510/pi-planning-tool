using PiPlanningBackend.Models;

namespace PiPlanningBackend.Repositories.Interfaces
{
    public interface IUserStoryRepository
    {
        Task<UserStory?> GetByAzureIdAsync(string azureId, int featureId);
        Task<UserStory?> GetByIdAsync(int id);
        Task AddAsync(UserStory story);
        Task UpdateAsync(UserStory story);
        Task SaveChangesAsync();
    }
}
