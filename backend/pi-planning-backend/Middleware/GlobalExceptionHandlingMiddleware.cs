using System.Text.Json;

namespace PiPlanningBackend.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {ExceptionType} - {Message}",
                    ex.GetType().Name, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message, details) = exception switch
            {
                // Validation & Input Errors (400) - More specific types FIRST
                ArgumentNullException ex
                    => (StatusCodes.Status400BadRequest, "Required argument is missing", ex.ParamName),

                ArgumentException ex
                    => (StatusCodes.Status400BadRequest, "Invalid argument provided", ex.Message),

                InvalidOperationException ex
                    => (StatusCodes.Status400BadRequest, "Invalid operation", ex.Message),

                // Entity Framework Errors
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                    => (StatusCodes.Status409Conflict, "Concurrency conflict - record was modified by another user", null),

                Microsoft.EntityFrameworkCore.DbUpdateException ex
                    => (StatusCodes.Status400BadRequest, "Database update failed", ex.InnerException?.Message),

                // Not Found (404)
                KeyNotFoundException ex
                    => (StatusCodes.Status404NotFound, "Resource not found", ex.Message),

                // Generic/Unhandled (500)
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred",
                    exception.InnerException?.Message ?? exception.Message)
            };

            context.Response.StatusCode = statusCode;

            var errorResponse = new
            {
                error = new
                {
                    message = message,
                    details = string.IsNullOrEmpty(details) ? null : details,
                    timestamp = DateTime.UtcNow
                }
            };

            return context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}