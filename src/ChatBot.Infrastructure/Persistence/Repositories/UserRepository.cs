using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            try
            {
                _logger.LogInformation(
                    "[USER] Buscando usuario por WhatsApp {WhatsApp}",
                    whatsapp);

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.WhatsAppNumber == whatsapp);

                if (user == null)
                {
                    _logger.LogInformation(
                        "[USER] Usuario no encontrado | WhatsApp={WhatsApp}",
                        whatsapp);
                }
                else
                {
                    _logger.LogInformation(
                        "[USER] Usuario encontrado | UserId={UserId} | WhatsApp={WhatsApp}",
                        user.Id,
                        whatsapp);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[USER][ERROR] Error consultando usuario por WhatsApp {WhatsApp}",
                    whatsapp);

                throw;
            }
        }

        public async Task AddAsync(TranxaUser user)
        {
            try
            {
                _logger.LogInformation(
                    "[USER] Creando usuario | UserId={UserId} | WhatsApp={WhatsApp}",
                    user.Id,
                    user.WhatsAppNumber);

                await _context.Users.AddAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[USER][ERROR] Error creando usuario | WhatsApp={WhatsApp}",
                    user.WhatsAppNumber);

                throw;
            }
        }

        public Task UpdateAsync(TranxaUser user)
        {
            try
            {
                _logger.LogInformation(
                    "[USER] Actualizando usuario | UserId={UserId} | WhatsApp={WhatsApp}",
                    user.Id,
                    user.WhatsAppNumber);

                _context.Users.Update(user);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[USER][ERROR] Error actualizando usuario | UserId={UserId}",
                    user.Id);

                throw;
            }
        }
    }
}