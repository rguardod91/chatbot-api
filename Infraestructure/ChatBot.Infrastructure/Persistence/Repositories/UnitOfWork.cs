using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Infrastructure.Persistence.Context;

namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TranxaDbContext _context;

        public UnitOfWork(TranxaDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public void Dispose()
            => _context.Dispose();
    }
}
