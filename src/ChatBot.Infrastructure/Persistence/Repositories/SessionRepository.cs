using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly TranxaDbContext _context;
        private readonly ILogger<SessionRepository> _logger;

        public SessionRepository(
            TranxaDbContext context,
            ILogger<SessionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TranxaSession?> GetActiveSessionAsync(Guid userId)
        {
            return await _context.Sessions
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Status.Equals("Active"));
        }

        public async Task AddAsync(TranxaSession session)
        {
            _logger.LogInformation("Creating session for user {UserId}", session.UserId);

            await _context.Sessions.AddAsync(session);
        }

        public Task UpdateAsync(TranxaSession session)
        {
            _context.Sessions.Update(session);

            return Task.CompletedTask;
        }
    }
}