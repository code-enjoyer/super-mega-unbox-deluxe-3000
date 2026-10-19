using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using SuperMegaUnboxDeluxe.Api.Correlation;
using SuperMegaUnboxDeluxe.Api.ErrorHandling;
using SuperMegaUnboxDeluxe.Api.HealthChecks;
using SuperMegaUnboxDeluxe.Api.Logging;
using SuperMegaUnboxDeluxe.Api.Settings;
using SuperMegaUnboxDeluxe.Api.Telemetry;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Infrastructure;
using SuperMegaUnboxDeluxe.Infrastructure.Extensions.ConfigurationExtensions;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;

namespace SuperMegaUnboxDeluxe.Api;

public static class Program
{
    private const string ClientCorsPolicyName = "SuperMegaUnboxDeluxeClient";

    public static async Task<int> Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        Log.Information("Application starting.");

        try
        {
            // Defaults include development-only User Secrets, environment variables,
            // and command-line arguments (highest priority).
            var migrate = args.Contains("--migrate", StringComparer.Ordinal);
            var builder = WebApplication.CreateBuilder(args.Where(argument => argument != "--migrate").ToArray());

            builder.Host.ConfigureLogging();

            Log.Information("Configuring services...");

            ConfigureServices(builder.Services, builder.Configuration);

            Log.Information("Configured services.");

            await using var app = builder.Build();

            if (migrate)
            {
                // A one-shot command, never an automatic migration in normal API startup.
                await using var scope = app.Services.CreateAsyncScope();
                var database = scope.ServiceProvider.GetRequiredService<SmudDbContext>();
                await database.Database.MigrateAsync();
                return 0;
            }

            Log.Information("Configuring application...");

            ConfigureApplication(app);

            Log.Information("Configured application.");

            Log.Information("Running application...");

            await app.RunAsync();

            return 0;
        }
        catch (HostAbortedException)
        {
            // EF tooling stops the host after retrieving its service provider.
            return 0;
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Application terminated unexpectedly.");

            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi(options =>
        {
            options.AddScalarTransformers();
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddSettings(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddSingleton<CorrelationIdMiddleware>();
        services.AddSmudHealthChecks();
        services.AddSmudTelemetry(configuration);
        services.AddCors(options =>
        {
            options.AddPolicy(ClientCorsPolicyName, policy => policy
                .WithOrigins(configuration.GetRequiredConnectionString(Infrastructure.Constants.ConnectionStringNames.SmudClient))
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.AllowOutOfOrderMetadataProperties = true;
            });
    }

    private static void ConfigureApplication(WebApplication app)
    {
        var apiSettings = app.Services.GetRequiredService<IOptions<ApiSettings>>().Value;

        app.UseCorrelationId();
        app.UseLogging();

        if (apiSettings.EnableOpenApi)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        if (apiSettings.EnableDeveloperExceptionPage)
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler();
        }

        app.UseCors(ClientCorsPolicyName);
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseStatusCodePages();
        app.MapGet("/", () => Results.Redirect(apiSettings.EnableOpenApi ? "/scalar" : "/api/status"));
        app.MapHealthChecks("/health", new HealthCheckOptions());
        app.MapControllers();
    }

    private static void AppUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        Log.Error(exception, $"Encountered unhandled exception.");
    }
}
