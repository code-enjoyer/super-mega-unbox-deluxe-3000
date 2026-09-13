using Microsoft.Extensions.Hosting;
using Serilog;
using SuperMegaUnboxDeluxe.Api.Settings;

namespace SuperMegaUnboxDeluxe.Api.Logging;

public static class LoggingHostBuilderExtensions
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty(Application.Constants.Logging.ApplicationName,
                    context.Configuration.GetValue((ApplicationSettings settings) => settings.Name));
        });
    }
}
