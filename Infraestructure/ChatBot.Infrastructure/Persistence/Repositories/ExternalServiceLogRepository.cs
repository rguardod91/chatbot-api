using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;


namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class ExternalServiceLogRepository : IExternalServiceLogRepository
    {
        private readonly TranxaDbContext _context;

        public ExternalServiceLogRepository(TranxaDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TranxaExternalServiceLog log)
        {
            await _context.ExternalServiceLogs.AddAsync(log);
        }
    }
}