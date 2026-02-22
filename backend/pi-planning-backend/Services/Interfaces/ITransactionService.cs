namespace PiPlanningBackend.Services.Interfaces
{
    /// <summary>
    /// Service for managing database transactions.
    /// Ensures multi-step operations either complete fully or rollback entirely.
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// Execute an async operation within a database transaction.
        /// Automatically commits on success or rolls back on exception.
        /// </summary>
        /// <param name="operation">Async operation to execute within transaction</param>
        /// <exception cref="Exception">Rethrows any exception from the operation after rollback</exception>
        Task ExecuteInTransactionAsync(Func<Task> operation);

        /// <summary>
        /// Execute an async operation within a database transaction and return result.
        /// Automatically commits on success or rolls back on exception.
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Async operation to execute within transaction</param>
        /// <returns>Result from the operation</returns>
        /// <exception cref="Exception">Rethrows any exception from the operation after rollback</exception>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }
}
