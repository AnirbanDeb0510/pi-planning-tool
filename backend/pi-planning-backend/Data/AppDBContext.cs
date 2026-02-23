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

            // ---------------------
            // Board relationships
            // ---------------------
            _ = modelBuilder.Entity<Board>()
                .HasMany(b => b.Sprints)
                .WithOne(s => s.Board!)
                .HasForeignKey(s => s.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = modelBuilder.Entity<Board>()
                .HasMany(b => b.Features)
                .WithOne(f => f.Board!)
                .HasForeignKey(f => f.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = modelBuilder.Entity<Board>()
                .HasMany(b => b.TeamMembers)
                .WithOne(t => t.Board!)
                .HasForeignKey(t => t.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------
            // Feature → UserStory
            // ---------------------
            _ = modelBuilder.Entity<Feature>()
                .HasMany(f => f.UserStories)
                .WithOne(us => us.Feature!)
                .HasForeignKey(us => us.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------
            // Sprint → TeamMemberSprint & UserStory
            // ---------------------
            _ = modelBuilder.Entity<Sprint>()
                .HasMany(s => s.TeamMemberSprints)
                .WithOne(tms => tms.Sprint!)
                .HasForeignKey(tms => tms.SprintId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = modelBuilder.Entity<Sprint>()
                .HasMany<UserStory>()
                .WithOne()
                .HasForeignKey(us => us.SprintId)
                .OnDelete(DeleteBehavior.NoAction); // we don’t want stories deleted if sprint is deleted

            _ = modelBuilder.Entity<UserStory>()
                .HasOne(us => us.Sprint)
                .WithMany(s => s.UserStories)
                .HasForeignKey(us => us.SprintId)
                .OnDelete(DeleteBehavior.NoAction);

            // ---------------------
            // TeamMember → TeamMemberSprint
            // ---------------------
            _ = modelBuilder.Entity<TeamMember>()
                .HasMany(t => t.TeamMemberSprints)
                .WithOne(tms => tms.TeamMember!)
                .HasForeignKey(tms => tms.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------------------
            // Indexes
            // ---------------------
            _ = modelBuilder.Entity<Feature>().HasIndex(f => f.AzureId);
            _ = modelBuilder.Entity<UserStory>().HasIndex(u => u.AzureId);
            _ = modelBuilder.Entity<UserStory>().HasIndex(u => u.SprintId);
            _ = modelBuilder.Entity<TeamMemberSprint>().HasIndex(t => new { t.TeamMemberId, t.SprintId });
            _ = modelBuilder.Entity<Sprint>().HasIndex(s => s.BoardId);
            _ = modelBuilder.Entity<Feature>().HasIndex(f => f.BoardId);
            _ = modelBuilder.Entity<TeamMember>().HasIndex(t => t.BoardId);

            // Ignore transient SignalR presence
            _ = modelBuilder.Ignore<CursorPresence>();
        }

    }
}
