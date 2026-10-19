using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Application.Extensions.ConfigurationExtensions;
using SuperMegaUnboxDeluxe.Application.Repositories;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;
using SuperMegaUnboxDeluxe.Infrastructure.Items;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

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
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IItemBaseRepository, ItemBaseRepository>();
        services.AddSingleton<IRandomSource, SystemRandomSource>();

        return services;
    }
}
