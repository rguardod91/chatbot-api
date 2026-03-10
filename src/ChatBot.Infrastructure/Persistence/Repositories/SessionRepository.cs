using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
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
            try
            {
                _logger.LogInformation(
                    "[SESSION] Buscando sesión activa para usuario {UserId}",
                    userId);

                var session = await _context.Sessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.Status == SessionStatus.Active);

                if (session == null)
                {
                    _logger.LogInformation(
                        "[SESSION] No existe sesión activa para usuario {UserId}",
                        userId);
                }
                else
                {
                    _logger.LogInformation(
                        "[SESSION] Sesión activa encontrada | SessionId={SessionId} | UserId={UserId}",
                        session.Id,
                        userId);
                }

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SESSION][ERROR] Error consultando sesión activa para usuario {UserId}",
                    userId);

                throw;
            }
        }

        public async Task AddAsync(TranxaSession session)
        {
            try
            {
                _logger.LogInformation(
                    "[SESSION] Creando nueva sesión | SessionId={SessionId} | UserId={UserId}",
                    session.Id,
                    session.UserId);

                await _context.Sessions.AddAsync(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SESSION][ERROR] Error creando sesión | UserId={UserId}",
                    session.UserId);

                throw;
            }
        }

        public Task UpdateAsync(TranxaSession session)
        {
            try
            {
                _logger.LogInformation(
                    "[SESSION] Actualizando sesión | SessionId={SessionId} | UserId={UserId}",
                    session.Id,
                    session.UserId);

                _context.Sessions.Update(session);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SESSION][ERROR] Error actualizando sesión | SessionId={SessionId}",
                    session.Id);

                throw;
            }
        }

        public async Task<TranxaSession?> GetActiveSessionAsync(Guid userId, int timeoutMinutes)
        {
            var limit = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

            var session = await _context.Sessions
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Status == SessionStatus.Active);

            if (session == null)
                return null;

            if (session.LastActivityAt < limit)
            {
                _logger.LogInformation(
                    "[SESSION] Sesión expirada | SessionId={SessionId}",
                    session.Id);

                session.Status = SessionStatus.Expired;
                session.UpdatedAt = DateTime.UtcNow;

                _context.Sessions.Update(session);

                await _context.SaveChangesAsync();

                return null;
            }

            return session;
        }

        public async Task ExpireSessionAsync(TranxaSession session)
        {
            _logger.LogInformation(
                "[SESSION] Expirando sesión {SessionId} por timeout",
                session.Id);

            session.Status = SessionStatus.Expired;
            session.UpdatedAt = DateTime.UtcNow;

            _context.Sessions.Update(session);
        }
    }
}