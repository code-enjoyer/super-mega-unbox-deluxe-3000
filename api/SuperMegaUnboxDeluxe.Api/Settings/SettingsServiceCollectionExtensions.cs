using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SuperMegaUnboxDeluxe.Api.Settings;

public static class SettingsServiceCollectionExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureSettings<ApplicationSettings>();
        ConfigureSettings<ApiSettings>();
        ConfigureSettings<FeatureFlags>();

        return services;

        void ConfigureSettings<TSettings>() where TSettings : class, ISettings
        {
            services.Configure<TSettings>(configuration.GetSection(TSettings.SettingsName));
        }
    }
}
