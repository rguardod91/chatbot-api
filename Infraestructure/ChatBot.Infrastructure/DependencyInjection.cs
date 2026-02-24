using ChatBot.Application.Interfaces.External;
using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Application.Interfaces.Services;
using ChatBot.Infrastructure.ExternalServices.Base;
using ChatBot.Infrastructure.ExternalServices.Services;
using ChatBot.Infrastructure.ExternalServices.Telegram;
using ChatBot.Infrastructure.ExternalServices.WhatsApp;
using ChatBot.Infrastructure.Logging;
using ChatBot.Infrastructure.Logging.Interfaces;
using ChatBot.Infrastructure.Persistence.Context;
using ChatBot.Infrastructure.Persistence.Repositories;
using ChatBot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ConversationStateService = ChatBot.Infrastructure.ExternalServices.Services.ConversationStateService;

namespace ChatBot.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<TranxaDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TranxaDbContext>());

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<ITranxaSessionRepository, TranxaSessionRepository>();

            services.AddScoped<ISessionManager, SessionManager>();
            services.AddScoped<IConversationStateService, ConversationStateService>();

            services.AddScoped<IBotConversationEngine, BotConversationEngine>();

            services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));

            services.AddHttpClient<IHttpService, HttpService>();

            services.AddHttpClient<ITranxaTokenService, TranxaTokenService>(client =>
            {
                client.BaseAddress = new Uri(configuration["Tranxa:BaseUrl"]!);
            });

            services.AddHttpClient<ITranxaService, TranxaService>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(config["Tranxa:BaseUrlUltraRed"]!);
            });

            services.AddHttpClient<IWhatsAppService, WhatsAppService>();
            services.AddHttpClient<ITelegramService, TelegramService>();


            services.AddScoped<ITranxaAuditLogRepository, TranxaAuditLogRepository>();


            return services;
        }

    }
}
