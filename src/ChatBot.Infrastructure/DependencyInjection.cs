using Amazon.SecretsManager;
using ChatBot.Application.Configuration;
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
using Microsoft.Extensions.Logging;

namespace ChatBot.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ========================
            // AWS CONFIGURATION
            // ========================

            services.Configure<AwsSettings>(
                configuration.GetSection("AWS"));

            services.AddDefaultAWSOptions(configuration.GetAWSOptions());
            services.AddAWSService<IAmazonSecretsManager>();

            services.AddScoped<ISecretsManagerService, SecretsManagerService>();


            // ========================
            // DATABASE
            // ========================

            services.AddDbContext<TranxaDbContext>((sp, options) =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    var logger = loggerFactory.CreateLogger("DatabaseConfig");
                    logger.LogWarning("ConnectionStrings:DefaultConnection está vacío.");
                }

                options.UseSqlServer(connectionString)
                       .UseLoggerFactory(loggerFactory)
                       .EnableDetailedErrors();

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            services.AddScoped<DbContext>(sp =>
                sp.GetRequiredService<TranxaDbContext>());


            // ========================
            // REPOSITORIES
            // ========================

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<ITranxaSessionRepository, TranxaSessionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISessionRepository, SessionRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<ISessionStateRepository, SessionStateRepository>();
            services.AddScoped<IAuditEventRepository, AuditEventRepository>();
            services.AddScoped<IExternalServiceLogRepository, ExternalServiceLogRepository>();
            services.AddScoped<ISystemLogRepository, SystemLogRepository>();


            // ========================
            // CORE SERVICES
            // ========================

            services.AddScoped<ISessionManager, SessionManager>();

            services.AddScoped<IConversationStateService,
                ExternalServices.Services.ConversationStateService>();

            services.AddScoped<IBotConversationEngine, BotConversationEngine>();


            // ========================
            // LOGGING
            // ========================

            services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));


            // ========================
            // HTTP BASE SERVICE
            // ========================

            services.AddHttpClient<IHttpService, HttpService>();


            // ========================
            // TRANXA TOKEN SERVICE
            // ========================

            services.AddHttpClient<ITranxaTokenService, TranxaTokenService>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<TranxaTokenService>>();

                var baseUrl = config["Tranxa:BaseUrl"];

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    logger.LogWarning("Tranxa:BaseUrl no está configurado.");
                }
                else
                {
                    client.BaseAddress = new Uri(baseUrl);
                    logger.LogInformation("TranxaTokenService configurado con BaseUrl {BaseUrl}", baseUrl);
                }
            });


            // ========================
            // TRANXA MAIN SERVICE
            // ========================

            services.AddHttpClient<ITranxaService, TranxaService>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<TranxaService>>();

                var baseUrl = config["Tranxa:BaseUrlUltraRed"];

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    logger.LogWarning("Tranxa:BaseUrlUltraRed no está configurado.");
                }
                else
                {
                    client.BaseAddress = new Uri(baseUrl);
                    logger.LogInformation("TranxaService configurado con BaseUrl {BaseUrl}", baseUrl);
                }
            });


            // ========================
            // WHATSAPP SERVICE
            // ========================

            services.AddHttpClient<IWhatsAppService, WhatsAppService>((sp, client) =>
            {
                var logger = sp.GetRequiredService<ILogger<WhatsAppService>>();
                logger.LogInformation("WhatsAppService HttpClient inicializado.");
            });


            // ========================
            // TELEGRAM SERVICE
            // ========================

            services.AddHttpClient<ITelegramService, TelegramService>((sp, client) =>
            {
                var logger = sp.GetRequiredService<ILogger<TelegramService>>();
                logger.LogInformation("TelegramService HttpClient inicializado.");
            });


            return services;
        }
    }
}