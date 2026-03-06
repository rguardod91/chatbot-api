using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class AuditEventRepository : IAuditEventRepository
    {
        private readonly TranxaDbContext _context;
        private readonly ILogger<AuditEventRepository> _logger;

        public AuditEventRepository(
            TranxaDbContext context,
            ILogger<AuditEventRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(TranxaAuditEvent auditEvent)
        {
            _logger.LogInformation(
                "Audit event | type={EventType} | session={SessionId}",
                auditEvent.EventType,
                auditEvent.SessionId);

            await _context.AuditEvents.AddAsync(auditEvent);
        }
    }
}