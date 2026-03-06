using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface IExternalServiceLogRepository
    {
        Task AddAsync(TranxaExternalServiceLog log);
    }
}
