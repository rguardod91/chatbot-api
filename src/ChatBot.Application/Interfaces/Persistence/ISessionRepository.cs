using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface ISessionRepository
    {
        Task<TranxaSession?> GetActiveSessionAsync(Guid userId);

        Task AddAsync(TranxaSession session);

        Task UpdateAsync(TranxaSession session);

        Task<TranxaSession?> GetActiveSessionAsync(Guid userId, int timeoutMinutes);
        Task ExpireSessionAsync(TranxaSession session);
    }
}
