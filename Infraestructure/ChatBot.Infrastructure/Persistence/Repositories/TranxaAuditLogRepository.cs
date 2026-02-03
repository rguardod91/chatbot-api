using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;

namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class TranxaAuditLogRepository : ITranxaAuditLogRepository
    {
        private readonly TranxaDbContext _context;

        public TranxaAuditLogRepository(TranxaDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TranxaAuditLog log)
        {
            await _context.TranxaAuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}
