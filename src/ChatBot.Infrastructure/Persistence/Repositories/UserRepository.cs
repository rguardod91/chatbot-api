using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ChatBot.Infrastructure.Persistence.Repositories
{

    public class UserRepository : IUserRepository
    {
        private readonly TranxaDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(
            TranxaDbContext context,
            ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TranxaUser?> GetByWhatsAppAsync(string whatsapp)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.WhatsAppNumber == whatsapp);
        }

        public async Task AddAsync(TranxaUser user)
        {
            _logger.LogInformation("Creating user {Whatsapp}", user.WhatsAppNumber);

            await _context.Users.AddAsync(user);
        }

        public Task UpdateAsync(TranxaUser user)
        {
            _context.Users.Update(user);

            return Task.CompletedTask;
        }
    }
}