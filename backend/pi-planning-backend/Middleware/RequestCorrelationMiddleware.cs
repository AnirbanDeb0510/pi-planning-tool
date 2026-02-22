namespace PiPlanningBackend.Middleware
{
    /// <summary>
    /// Middleware for adding and tracking correlation IDs across requests.
    /// Extracts correlation ID from X-Correlation-ID header or generates a new one.
    /// Stores it in HttpContext.Items for access throughout the request lifecycle.
    /// </summary>
    public class RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<RequestCorrelationMiddleware> _logger = logger;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        private const string CorrelationIdHttpContextItemName = "CorrelationId";

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract correlation ID from header or generate new one
            var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

            // Store correlation ID in HttpContext.Items for access in services/controllers
            context.Items[CorrelationIdHttpContextItemName] = correlationId;

            // Add correlation ID to response headers
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;

            // Log request start
            _logger.LogInformation(
                "Request started | CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path} | QueryString: {QueryString}",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString);

            try
            {
                await _next(context);

                // Log request completion
                _logger.LogInformation(
                    "Request completed | CorrelationId: {CorrelationId} | StatusCode: {StatusCode}",
                    correlationId,
                    context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Request failed | CorrelationId: {CorrelationId} | Exception: {ExceptionType}",
                    correlationId,
                    ex.GetType().Name);
                throw;
            }
        }

        /// <summary>
        /// Helper method to retrieve correlation ID from HttpContext.
        /// Useful in services/controllers that have access to IHttpContextAccessor.
        /// </summary>
        public static string? GetCorrelationId(HttpContext context)
        {
            return context.Items.TryGetValue(CorrelationIdHttpContextItemName, out var correlationId)
                ? correlationId?.ToString()
                : null;
        }
    }
}
