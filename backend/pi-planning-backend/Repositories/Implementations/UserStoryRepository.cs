using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;

namespace PiPlanningBackend.Repositories.Implementations
{
    public class UserStoryRepository(AppDbContext db) : IUserStoryRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<UserStory?> GetByAzureIdAsync(string azureId, int featureId)
        {
            return await _db.UserStories.FirstOrDefaultAsync(u => u.AzureId == azureId && u.FeatureId == featureId);
        }

        public async Task<UserStory?> GetByIdAsync(int id)
        {
            return await _db.UserStories.FindAsync(id);
        }

        public async Task<UserStory?> GetByIdWithDetailsAsync(int id)
        {
            return await _db.UserStories
                        .Include(s => s.Feature)
                        .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(UserStory story)
        {
            _ = await _db.UserStories.AddAsync(story);
        }

        public Task UpdateAsync(UserStory story)
        {
            _ = _db.UserStories.Update(story);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            _ = await _db.SaveChangesAsync();
        }
    }
}
