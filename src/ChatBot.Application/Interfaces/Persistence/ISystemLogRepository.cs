using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface ISystemLogRepository
    {
        Task AddAsync(TranxaSystemLog log);
    }
}