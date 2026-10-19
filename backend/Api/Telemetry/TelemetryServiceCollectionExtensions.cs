using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SuperMegaUnboxDeluxe.Api.Settings;

namespace SuperMegaUnboxDeluxe.Api.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddSmudTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSettings<TelemetrySettings>();
        settings.Validate();
        if (!settings.Enabled)
        {
            return services;
        }

        var serviceName = configuration.GetValue((ApplicationSettings application) => application.Name);
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new InvalidOperationException("Application:Name is required for telemetry.");
        }

        // Logs use only Serilog's OTLP sink. Do not register an SDK logging exporter.
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("Npgsql")
                .AddOtlpExporter(options => ConfigureExporter(options, settings, "traces")))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(options => ConfigureExporter(options, settings, "metrics")));

        return services;
    }

    private static void ConfigureExporter(OtlpExporterOptions options, TelemetrySettings settings, string signal)
    {
        options.Endpoint = new Uri(settings.GetEndpoint(), "v1/" + signal);
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Headers = "x-otlp-api-key=" + settings.ApiKey;
    }
}
