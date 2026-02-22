namespace PiPlanningBackend.Middleware
{
    public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Extract correlation ID from context (set by RequestCorrelationMiddleware)
                var correlationId = context.Items.TryGetValue("CorrelationId", out var correlationIdObj)
                    ? correlationIdObj?.ToString() ?? "NO_CORRELATION_ID"
                    : "NO_CORRELATION_ID";

                _logger.LogError(ex,
                    "Unhandled exception | CorrelationId: {CorrelationId} | ExceptionType: {ExceptionType} | Message: {Message}",
                    correlationId, ex.GetType().Name, ex.Message);
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
                    message,
                    details = string.IsNullOrEmpty(details) ? null : details,
                    timestamp = DateTime.UtcNow
                }
            };

            return context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}