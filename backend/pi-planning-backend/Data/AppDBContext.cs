using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Models;

namespace PiPlanningBackend.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
    {
        public DbSet<Board> Boards { get; set; } = null!;
        public DbSet<Sprint> Sprints { get; set; } = null!;
        public DbSet<Feature> Features { get; set; } = null!;
        public DbSet<UserStory> UserStories { get; set; } = null!;
        public DbSet<TeamMember> TeamMembers { get; set; } = null!;
        public DbSet<TeamMemberSprint> TeamMemberSprints { get; set; } = null!;
        public DbSet<CursorPresence> CursorPresences { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes: helpful for AzureId lookups
            modelBuilder.Entity<Feature>()
                .HasIndex(f => f.AzureId)
                .IsUnique(false);

            modelBuilder.Entity<UserStory>()
                .HasIndex(u => u.AzureId)
                .IsUnique(false);

            modelBuilder.Entity<UserStory>()
                .HasIndex(u => u.CurrentSprintId);

            modelBuilder.Entity<TeamMemberSprint>()
                .HasIndex(t => new { t.TeamMemberId, t.SprintId });
        }
    }
}
