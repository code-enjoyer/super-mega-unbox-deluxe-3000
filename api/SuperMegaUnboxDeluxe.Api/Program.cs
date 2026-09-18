using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
using SuperMegaUnboxDeluxe.Application.Extensions.ConfigurationExtensions;
using SuperMegaUnboxDeluxe.Infrastructure;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Api;

public static class Program
{
    private const string SmudClientCorsPolicyName = "SmudClient";

    public static async Task<int> Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;
        Log.Information("Starting");
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = Directory.GetCurrentDirectory(),
                EnvironmentName = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AspNetEnvironment)
            });

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets(typeof(Program).Assembly);

            builder.Host.ConfigureLogging();

            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Initialize database migrations based on configuration
            var apiSettings = app.Services.GetRequiredService<IOptions<ApiSettings>>().Value;
            if (apiSettings.AutoApplyMigrations)
            {
                await app.Services.InitializeDatabaseAsync();
            }

            ConfigureApplication(app);

            await app.RunAsync();

            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"An error occurred: {exception.Message}");
            Console.WriteLine(exception?.StackTrace);

            return 1;
        }
        finally
        {

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
        services.AddInfrastructure(configuration);
        services.AddSingleton<CorrelationIdMiddleware>();
        services.AddSmudHealthChecks();
        services.AddCors(options =>
        {
            options.AddPolicy(name: SmudClientCorsPolicyName, policy =>
            {
                policy.WithOrigins(configuration.GetRequiredConnectionString(Infrastructure.Constants.ConnectionStringNames.SmudClient))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
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
            app.UseExceptionHandler("/error");
        }

        app.UseCors(SmudClientCorsPolicyName);
        app.UseHttpsRedirection();
        app.UseCorrelationId();
        app.UseAuthorization();
        app.UseLogging();
        app.UseStatusCodePages();
        app.MapGet("/", () => Results.Redirect("/scalar"));
        app.MapControllers();
    }

    private static void AppUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        Log.Error(exception, $"Encountered unhandled exception.");
    }
}
