using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TranxaDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWork(
            TranxaDbContext context,
            ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("===== DATABASE SAVE OPERATION STARTED =====");

                var pendingChanges = _context.ChangeTracker
                    .Entries()
                    .Count(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged);

                _logger.LogInformation("Entities pending changes: {PendingChanges}", pendingChanges);

                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    _logger.LogInformation(
                        "Entity: {Entity} | State: {State}",
                        entry.Entity.GetType().Name,
                        entry.State);
                }

                var result = await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Database SaveChanges executed. Rows affected: {Rows}", result);
                _logger.LogInformation("===== DATABASE SAVE OPERATION FINISHED =====");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing SaveChangesAsync");
                throw;
            }
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing DbContext");
            _context.Dispose();
        }
    }
}