using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PiPlanningBackend.Data;

namespace PiPlanningBackend.Migrations.SqlServer;

/// <summary>
/// Design-time factory for creating AppDbContext configured for SQL Server.
/// This allows EF Core tools to generate migrations without needing the full application startup.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Use a dummy connection string for design-time operations
        // The actual connection string will be used at runtime
        optionsBuilder.UseSqlServer("Server=localhost;Database=piplanningdb;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;",
            sqlServerOptions => sqlServerOptions.MigrationsAssembly("pi-planning-backend.migrations.sqlserver"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
