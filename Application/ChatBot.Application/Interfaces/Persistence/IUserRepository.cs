using ChatBot.Domain.Entities;

namespace ChatBot.Application.Interfaces.Persistence
{
    public interface IUserRepository
    {
        Task<TranxaUser?> GetByWhatsAppAsync(string whatsapp);

        Task AddAsync(TranxaUser user);

        Task UpdateAsync(TranxaUser user);
    }
}
