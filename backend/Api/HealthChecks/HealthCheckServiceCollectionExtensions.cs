using Microsoft.Extensions.DependencyInjection;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;

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
