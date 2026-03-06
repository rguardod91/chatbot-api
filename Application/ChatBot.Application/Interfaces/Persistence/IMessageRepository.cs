using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface IMessageRepository
    {
        Task AddAsync(TranxaMessage message);
    }
}
