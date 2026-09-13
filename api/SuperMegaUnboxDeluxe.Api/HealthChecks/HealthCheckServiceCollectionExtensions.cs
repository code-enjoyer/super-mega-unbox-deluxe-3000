using SuperMegaUnboxDeluxe.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace SuperMegaUnboxDeluxe.Api.HealthChecks;

public static class HealthCheckServiceCollectionExtensions
{
    public static IServiceCollection AddSmudHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<SmudDbContext>(HealthCheckNames.SmudDatabase);
        
        return services;
    }
}
