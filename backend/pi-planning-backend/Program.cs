using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Filters;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Middleware;
using PiPlanningBackend.Services.Interfaces;
using PiPlanningBackend.Services.Implementations;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Repositories.Implementations;
using System.Runtime.Loader;

// Register assembly resolver for migration assemblies
// This allows EF Core to load migration assemblies from the application directory
AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
{
    if (assemblyName.Name != null &&
        assemblyName.Name.StartsWith("pi-planning-backend.migrations."))
    {
        string assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(assemblyPath))
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }
    }
    return null;
};

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string[] configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
bool allowAllOrigins = configuredCorsOrigins.Contains("*");

bool IsCorsOriginAllowed(string origin)
{
    if (allowAllOrigins)
    {
        return true;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri))
    {
        return false;
    }

    if (!originUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
        && !originUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    // Allow any localhost port in all environments
    return originUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || originUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || originUri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase) || configuredCorsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
}

// Add services
builder.Services.AddControllers(options =>
{
    // Register ValidateModelStateFilter globally - applies to all controller actions
    _ = options.Filters.Add<ValidateModelStateFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpContextAccessor (needed for accessing correlation ID from services)
builder.Services.AddHttpContextAccessor();

// DbContext
string databaseProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        _ = options.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.MigrationsAssembly("pi-planning-backend.migrations.sqlserver")
        );
        return;
    }

    _ = options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("pi-planning-backend.migrations.postgres")
    );
});

// SignalR
builder.Services.AddSignalR();

// AzureBoardsService with HttpClient
builder.Services.AddHttpClient<IAzureBoardsService, AzureBoardsService>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IFeatureRepository, FeatureRepository>();
builder.Services.AddScoped<IUserStoryRepository, UserStoryRepository>();
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<ISprintService, SprintService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
// Transaction service (for wrapping multi-step operations)
builder.Services.AddScoped<ITransactionService, TransactionService>();
// Correlation ID provider (for request tracking across services)
builder.Services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();


// CORS (config-driven for local/dev/prod)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(IsCorsOriginAllowed)
            .AllowCredentials());
});

WebApplication app = builder.Build();

bool swaggerEnabled = builder.Configuration.GetValue<bool?>("Swagger:Enabled")
    ?? app.Environment.IsDevelopment();

app.Logger.LogInformation(
    "Active database provider: {DatabaseProvider}",
    databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ? "SqlServer" : "PostgreSQL");

app.Logger.LogInformation(
    "CORS policy loaded | AllowAll: {AllowAll} | Origins: {Origins}",
    allowAllOrigins,
    allowAllOrigins ? "*" : string.Join(", ", configuredCorsOrigins));

// Apply migrations at startup (optional for Dev & safe if you control migrations)
// Note: EF Core automatically detects the active provider and applies only compatible migrations
//       from Migrations/ (PostgreSQL) or Migrations_SqlServer/ (SQL Server) folders
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    app.Logger.LogInformation("Starting database migration check...");
    try
    {
        db.Database.Migrate();
        app.Logger.LogInformation("Database migrations completed successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error applying database migrations: {Message}", ex.Message);
        throw;
    }
}

// Global exception handling (MUST be early in pipeline)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Request correlation tracking (for tracing requests across logs)
app.UseMiddleware<RequestCorrelationMiddleware>();

if (swaggerEnabled)
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseCors("Frontend");
// app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<PlanningHub>("/hub/planning");

app.Run();
