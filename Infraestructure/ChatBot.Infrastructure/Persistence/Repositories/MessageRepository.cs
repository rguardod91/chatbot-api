using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace ChatBot.Infrastructure.Persistence.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly TranxaDbContext _context;

        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(
            TranxaDbContext context,
            ILogger<MessageRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(TranxaMessage message)
        {
            try
            {
                _logger.LogInformation(
                    "Saving message | session={SessionId} | direction={Direction}",
                    message.SessionId,
                    message.Direction);

                await _context.Messages.AddAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving message");
                throw;
            }
        }
    }
}