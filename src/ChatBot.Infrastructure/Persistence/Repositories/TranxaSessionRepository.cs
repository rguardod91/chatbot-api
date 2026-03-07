using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class TranxaSessionRepository
        : Repository<TranxaSession>, ITranxaSessionRepository
    {
        public TranxaSessionRepository(TranxaDbContext context) : base(context) { }

        public async Task<TranxaSession?> GetActiveSessionByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(s => s.States)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SessionStatus.Active);
        }
    }
}
