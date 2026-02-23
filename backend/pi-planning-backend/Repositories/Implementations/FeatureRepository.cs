using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;

namespace PiPlanningBackend.Repositories.Implementations
{
    public class FeatureRepository(AppDbContext db) : IFeatureRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<Feature?> GetByAzureIdAsync(string azureId, int boardId)
        {
            return await _db.Features
                .Include(f => f.UserStories)
                .FirstOrDefaultAsync(f => f.AzureId == azureId && f.BoardId == boardId);
        }

        public async Task<Feature?> GetByIdAsync(int id)
        {
            return await _db.Features.Include(f => f.UserStories).FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<int> GetMaxPriorityAsync(int boardId)
        {
            int maxPriority = await _db.Features
                .Where(f => f.BoardId == boardId)
                .MaxAsync(f => f.Priority) ?? 0;
            return maxPriority;
        }

        public async Task AddAsync(Feature feature)
        {
            _ = await _db.Features.AddAsync(feature);
        }

        public Task UpdateAsync(Feature feature)
        {
            _ = _db.Features.Update(feature);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Feature feature)
        {
            _ = _db.Features.Remove(feature);
            return Task.CompletedTask;
        }

        public Task<List<UserStory>> GetUserStoriesByFeatureAsync(int featureId)
        {
            return _db.UserStories.Where(u => u.FeatureId == featureId).ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            _ = await _db.SaveChangesAsync();
        }
    }
}
