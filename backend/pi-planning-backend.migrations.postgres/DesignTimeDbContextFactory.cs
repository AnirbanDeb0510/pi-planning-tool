using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PiPlanningBackend.Data;

namespace PiPlanningBackend.Migrations.Postgres;

/// <summary>
/// Design-time factory for creating AppDbContext configured for PostgreSQL.
/// This allows EF Core tools to generate migrations without needing the full application startup.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Use a dummy connection string for design-time operations
        // The actual connection string will be used at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=piplanningdb;Username=piuser;Password=password",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("pi-planning-backend.migrations.postgres"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
