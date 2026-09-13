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
        // TODO: Set up local db to connect to

        return services;

        var connectionString = configuration.GetRequiredConnectionString(Constants.ConnectionStringNames.SmudDatabase);

        services.AddDbContext<SmudDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {

            });
        });
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SmudDbContext>());

        return services;
    }
}