using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Infrastructure.Persistence.Repositories
{

    public class SessionStateRepository : ISessionStateRepository
    {
        private readonly TranxaDbContext _context;

        public SessionStateRepository(TranxaDbContext context)
        {
            _context = context;
        }

        public async Task<TranxaSessionState?> GetBySessionIdAsync(Guid sessionId)
        {
            return await _context.SessionStates
                .FirstOrDefaultAsync(x => x.SessionId == sessionId);
        }

        public async Task SaveAsync(TranxaSessionState state)
        {
            var existing = await GetBySessionIdAsync(state.SessionId);

            if (existing == null)
                await _context.SessionStates.AddAsync(state);
            else
                _context.SessionStates.Update(state);
        }
    }
}