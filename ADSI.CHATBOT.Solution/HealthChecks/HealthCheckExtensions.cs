using ChatBot.Infrastructure.Persistence.Context;

namespace ChatBot.Api.HealthChecks
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddCustomHealthChecks(
            this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<TranxaDbContext>();

            return services;
        }
    }
}
