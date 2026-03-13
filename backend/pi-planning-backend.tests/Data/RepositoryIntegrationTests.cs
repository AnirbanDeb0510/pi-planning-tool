using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Implementations;
using Xunit;

namespace PiPlanningBackend.Tests.Data
{
    public class RepositoryIntegrationTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task BoardRepository_SearchBoardsAsync_AppliesFiltersAndSortsByCreatedAtDesc()
        {
            string dbName = Guid.NewGuid().ToString();
            await using AppDbContext context = CreateContext(dbName);
            BoardRepository repo = new(context);

            Board older = new()
            {
                Id = 1,
                Name = "PI Alpha",
                Organization = "Contoso",
                Project = "Project A",
                IsLocked = true,
                IsFinalized = false,
                CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            Board newer = new()
            {
                Id = 2,
                Name = "PI Alpha Increment",
                Organization = "Contoso",
                Project = "Project A",
                IsLocked = true,
                IsFinalized = false,
                CreatedAt = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc)
            };
            Board nonMatch = new()
            {
                Id = 3,
                Name = "PI Beta",
                Organization = "Fabrikam",
                Project = "Project B",
                IsLocked = false,
                IsFinalized = true,
                CreatedAt = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)
            };

            context.Boards.AddRange(older, newer, nonMatch);
            await context.SaveChangesAsync();

            IEnumerable<Board> result = await repo.SearchBoardsAsync(
                searchTerm: "PI Alpha",
                organization: "Contoso",
                project: "Project A",
                isLocked: true,
                isFinalized: false);

            List<Board> list = [.. result];
            Assert.Equal(2, list.Count);
            Assert.Equal(2, list[0].Id); // newer first
            Assert.Equal(1, list[1].Id);
        }

        [Fact]
        public async Task BoardRepository_GetBoardWithFullHierarchyAsync_LoadsNestedRelations()
        {
            string dbName = Guid.NewGuid().ToString();
            await using AppDbContext context = CreateContext(dbName);
            BoardRepository repo = new(context);

            Board board = new() { Id = 10, Name = "PI Board" };
            Sprint sprint = new() { Id = 100, BoardId = 10, Name = "Sprint 1" };
            Feature feature = new() { Id = 200, BoardId = 10, Title = "Feature 1" };
            UserStory story = new() { Id = 300, FeatureId = 200, SprintId = 100, Title = "Story 1" };
            TeamMember member = new() { Id = 400, BoardId = 10, Name = "Alice", IsDev = true };
            TeamMemberSprint tms = new() { Id = 500, TeamMemberId = 400, SprintId = 100, CapacityDev = 10, CapacityTest = 0 };

            context.Boards.Add(board);
            context.Sprints.Add(sprint);
            context.Features.Add(feature);
            context.UserStories.Add(story);
            context.TeamMembers.Add(member);
            context.TeamMemberSprints.Add(tms);
            await context.SaveChangesAsync();

            Board? loaded = await repo.GetBoardWithFullHierarchyAsync(10);

            Assert.NotNull(loaded);
            Assert.Single(loaded.Sprints);
            Assert.Single(loaded.Features);
            Assert.Single(loaded.Features.First().UserStories);
            Assert.Single(loaded.TeamMembers);
            Assert.Single(loaded.TeamMembers.First().TeamMemberSprints);
        }

        [Fact]
        public async Task FeatureRepository_GetMaxPriorityAsync_ReturnsZeroWhenNoFeatures()
        {
            string dbName = Guid.NewGuid().ToString();
            await using AppDbContext context = CreateContext(dbName);
            FeatureRepository repo = new(context);

            int max = await repo.GetMaxPriorityAsync(1);

            Assert.Equal(0, max);
        }

        [Fact]
        public async Task TeamRepository_GetTeamMemberSprintAsync_LoadsTeamMemberSprintAndBoard()
        {
            string dbName = Guid.NewGuid().ToString();
            await using AppDbContext context = CreateContext(dbName);
            TeamRepository repo = new(context);

            Board board = new() { Id = 1, Name = "PI Board" };
            Sprint sprint = new() { Id = 10, BoardId = 1, Name = "Sprint 1", Board = board };
            TeamMember member = new() { Id = 20, BoardId = 1, Name = "Alice" };
            TeamMemberSprint tms = new() { Id = 30, TeamMemberId = 20, SprintId = 10, CapacityDev = 8, CapacityTest = 2 };

            context.Boards.Add(board);
            context.Sprints.Add(sprint);
            context.TeamMembers.Add(member);
            context.TeamMemberSprints.Add(tms);
            await context.SaveChangesAsync();

            TeamMemberSprint? loaded = await repo.GetTeamMemberSprintAsync(10, 20);

            Assert.NotNull(loaded);
            Assert.NotNull(loaded.TeamMember);
            Assert.NotNull(loaded.Sprint);
            Assert.NotNull(loaded.Sprint.Board);
            Assert.Equal(1, loaded.Sprint.Board.Id);
        }
    }
}
