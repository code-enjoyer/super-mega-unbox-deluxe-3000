using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SuperMegaUnboxDeluxe.Api.Settings;

namespace SuperMegaUnboxDeluxe.Api.Logging;

public static class LoggingLoggerConfigurationExtensions
{
    public static LoggerConfiguration ConfigureLogger(this LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty(Infrastructure.Constants.Logging.ApplicationName,
                    configuration.GetValue((ApplicationSettings settings) => settings.Name));

        var telemetry = configuration.GetSettings<TelemetrySettings>();
        telemetry.Validate();
        if (telemetry.Enabled)
        {
            var serviceName = configuration.GetValue((ApplicationSettings application) => application.Name);
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new InvalidOperationException("Application:Name is required for telemetry.");
            }
            loggerConfiguration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = telemetry.GetEndpoint().AbsoluteUri;
                options.Protocol = OtlpProtocol.HttpProtobuf;
                options.Headers = new Dictionary<string, string> { ["x-otlp-api-key"] = telemetry.ApiKey };
                options.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = serviceName };
                options.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
            }, ignoreEnvironment: true);
        }

        return loggerConfiguration;
    }
}
