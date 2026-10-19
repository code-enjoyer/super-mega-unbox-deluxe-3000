using Microsoft.Extensions.DependencyInjection;
using SuperMegaUnboxDeluxe.Application.Items;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using System;

namespace SuperMegaUnboxDeluxe.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Construct eagerly so invalid catalog content prevents startup.
        services.AddSingleton<IItemCatalog>(new StarterItemCatalog());
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<ItemGenerator>();
        services.AddScoped<GenerateItemService>();
        return services;
    }
}
