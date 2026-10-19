using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using SuperMegaUnboxDeluxe.Infrastructure.Extensions.ConfigurationExtensions;
using SuperMegaUnboxDeluxe.Infrastructure.Items;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;

namespace SuperMegaUnboxDeluxe.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["Persistence:Password"] is { Length: > 0 } password
            ? new NpgsqlConnectionStringBuilder
            {
                Host = configuration["Persistence:Host"] ?? "postgres",
                Database = "smud_dev",
                Username = "smud_user",
                Password = password
            }.ConnectionString
            : configuration.GetRequiredConnectionString(Constants.ConnectionStringNames.SmudDatabase);

        services.AddDbContext<SmudDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SmudDbContext>());
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IItemQueries, ItemQueries>();
        services.AddScoped<IItemBaseRepository, ItemBaseRepository>();
        services.AddSingleton<IRandomSource, SystemRandomSource>();

        return services;
    }
}
