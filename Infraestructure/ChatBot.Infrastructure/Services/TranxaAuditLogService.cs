using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Application.Interfaces.Services;
using ChatBot.Domain.Entities;

namespace ChatBot.Infrastructure.Services
{
    public class TranxaAuditLogService : ITranxaAuditLogService
    {
        private readonly ITranxaAuditLogRepository _repo;

        public TranxaAuditLogService(ITranxaAuditLogRepository repo)
        {
            _repo = repo;
        }

        public async Task RegisterAsync(string userId, string action, string tokenId, string? result, string? details = null)
        {
            var log = new TranxaAuditLog
            {
                UserChannelId = userId,
                Action = action,
                TokenId = tokenId,
                Result = result,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(log);
        }
    }

}
