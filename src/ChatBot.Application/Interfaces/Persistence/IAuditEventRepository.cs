using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface IAuditEventRepository
    {
        Task AddAsync(TranxaAuditEvent auditEvent);
    }
}
