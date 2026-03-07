using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface ITranxaSessionRepository : IRepository<TranxaSession>
    {
        Task<TranxaSession?> GetActiveSessionByUserAsync(Guid userId);
    }
}
