using PiPlanningBackend.Middleware;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    /// <summary>
    /// Provides access to the current request's correlation ID from HttpContext.
    /// Encapsulates the logic for retrieving the correlation ID set by RequestCorrelationMiddleware.
    /// </summary>
    public class CorrelationIdProvider(IHttpContextAccessor httpContextAccessor) : ICorrelationIdProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// Gets the correlation ID for the current request.
        /// Retrieved from HttpContext.Items by RequestCorrelationMiddleware.
        /// </summary>
        public string? GetCorrelationId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            return RequestCorrelationMiddleware.GetCorrelationId(httpContext);
        }
    }
}
