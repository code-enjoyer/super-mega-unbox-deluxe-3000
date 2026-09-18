using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Extensions.ConfigurationExtensions;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;

namespace SuperMegaUnboxDeluxe.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString(Constants.ConnectionStringNames.SmudDatabase);

        services.AddDbContext<SmudDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SmudDbContext>());

        return services;
    }
}