using PiPlanningBackend.Data;
using PiPlanningBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace PiPlanningBackend.Services.Implementations
{
    /// <summary>
    /// Implementation of ITransactionService for managing database transactions.
    /// </summary>
    public class TransactionService(AppDbContext dbContext) : ITransactionService
    {
        private readonly AppDbContext _dbContext = dbContext;

        /// <summary>
        /// Execute an async operation within a database transaction.
        /// Automatically commits on success or rolls back on exception.
        /// </summary>
        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Execute an async operation within a database transaction and return result.
        /// Automatically commits on success or rolls back on exception.
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                T? result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
