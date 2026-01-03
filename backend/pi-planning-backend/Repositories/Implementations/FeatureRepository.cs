using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;

namespace PiPlanningBackend.Repositories.Implementations
{
    public class FeatureRepository : IFeatureRepository
    {
        private readonly AppDbContext _db;
        public FeatureRepository(AppDbContext db) => _db = db;

        public async Task<Feature?> GetByAzureIdAsync(string azureId, int boardId)
        {
            return await _db.Features
                .Include(f => f.UserStories)
                .FirstOrDefaultAsync(f => f.AzureId == azureId && f.BoardId == boardId);
        }

        public async Task<Feature?> GetByIdAsync(int id)
            => await _db.Features.Include(f => f.UserStories).FirstOrDefaultAsync(f => f.Id == id);

        public async Task AddAsync(Feature feature) => await _db.Features.AddAsync(feature);

        public Task UpdateAsync(Feature feature)
        {
            _db.Features.Update(feature);
            return Task.CompletedTask;
        }

        public Task<List<UserStory>> GetUserStoriesByFeatureAsync(int featureId)
            => _db.UserStories.Where(u => u.FeatureId == featureId).ToListAsync();

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
