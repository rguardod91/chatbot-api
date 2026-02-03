using ChatBot.Application.DTOs.Tranza.Models;

namespace ChatBot.Application.Interfaces.Services
{
    public interface ISessionManager
    {
        Task<SessionContext> GetSessionAsync(string userId);
        Task SaveSessionAsync(string userId, SessionContext context);
    }
}
