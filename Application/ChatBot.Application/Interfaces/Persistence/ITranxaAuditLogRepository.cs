using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface ITranxaAuditLogRepository
    {
        Task AddAsync(TranxaAuditLog log);
    }
}
