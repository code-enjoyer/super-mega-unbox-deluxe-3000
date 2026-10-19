using Microsoft.Extensions.Hosting;
using Serilog;

namespace SuperMegaUnboxDeluxe.Api.Logging;

public static class LoggingHostBuilderExtensions
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration.ConfigureLogger(context.Configuration);
        });
    }
}
