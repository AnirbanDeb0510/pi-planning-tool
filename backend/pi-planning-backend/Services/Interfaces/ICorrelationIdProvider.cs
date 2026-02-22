namespace PiPlanningBackend.Services.Interfaces
{
    /// <summary>
    /// Provides access to the current request's correlation ID.
    /// Abstracts the correlation ID from the request context.
    /// </summary>
    public interface ICorrelationIdProvider
    {
        /// <summary>
        /// Gets the correlation ID for the current request.
        /// Returns a GUID string that uniquely identifies the request for tracing.
        /// </summary>
        string? GetCorrelationId();
    }
}
