using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.Filters;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Middleware;
using PiPlanningBackend.Services.Interfaces;
using PiPlanningBackend.Services.Implementations;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Repositories.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers(options =>
{
    // Register ValidateModelStateFilter globally - applies to all controller actions
    options.Filters.Add<ValidateModelStateFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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


// CORS (dev convenience)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

// Apply migrations at startup (optional for Dev & safe if you control migrations)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Global exception handling (MUST be early in pipeline)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Request correlation tracking (for tracing requests across logs)
app.UseMiddleware<RequestCorrelationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
// app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<PlanningHub>("/hub/planning");

app.Run();
