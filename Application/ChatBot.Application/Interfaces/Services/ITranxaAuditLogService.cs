namespace ChatBot.Application.Interfaces.Services
{
    public interface ITranxaAuditLogService
    {
        Task RegisterAsync(string userId, string action, string tokenId, string? result, string? details = null);
    }

}
