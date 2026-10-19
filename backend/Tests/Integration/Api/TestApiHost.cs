using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Api;

// All HTTP tests exercise the production composition root and middleware.
internal sealed class TestApiHost : IAsyncDisposable
{
    private readonly ApiTestFactory _factory;
    public HttpClient Client { get; }

    private TestApiHost(Action<IServiceCollection> configure, string? connectionString)
    {
        _factory = new ApiTestFactory(configure: configure, connectionString: connectionString);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    }

    public static Task<TestApiHost> StartAsync(Action<IServiceCollection> configure, string? connectionString = null) =>
        Task.FromResult(new TestApiHost(configure, connectionString));

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
