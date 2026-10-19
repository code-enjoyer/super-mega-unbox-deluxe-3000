using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperMegaUnboxDeluxe.Api.Controllers;
using SuperMegaUnboxDeluxe.Application;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Api;

// Exercise real controller routing, model binding and serialization without booting unrelated host services.
internal sealed class TestApiHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    public HttpClient Client { get; }

    private TestApiHost(WebApplication app)
    {
        _app = app;
        Client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    }

    public static async Task<TestApiHost> StartAsync(Action<IServiceCollection> configure)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(ItemsController).Assembly.GetName().Name
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddApplication();
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers().AddApplicationPart(typeof(ItemsController).Assembly);
        configure(builder.Services);
        var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        return new TestApiHost(app);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
