using Microsoft.Extensions.DependencyInjection;

namespace ChatBot.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Aquí luego podrás registrar:
            // - Validators (FluentValidation)
            // - MediatR
            // - UseCases / Handlers
            // - Mapping profiles

            return services;
        }
    }
}
