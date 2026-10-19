using System;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Application.Items.GenerateItem;
using SuperMegaUnboxDeluxe.Application.Items.GetItemDetails;
using SuperMegaUnboxDeluxe.Application.Items.GetLootSources;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Construct eagerly so invalid catalog content prevents startup.
        services.AddSingleton<IItemCatalog>(new StarterItemCatalog());
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<ItemGenerator>();
        services.AddScoped<IValidator<GenerateItemCommand>, GenerateItemValidator>();
        services.AddScoped<GenerateItemHandler>();
        services.AddScoped<GetItemDetailsHandler>();
        services.AddScoped<GetLootSourcesHandler>();
        return services;
    }
}
