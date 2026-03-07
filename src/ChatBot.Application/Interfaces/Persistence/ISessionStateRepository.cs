using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface ISessionStateRepository
    {
        Task<TranxaSessionState?> GetBySessionIdAsync(Guid sessionId);

        Task SaveAsync(TranxaSessionState state);
    }
}
