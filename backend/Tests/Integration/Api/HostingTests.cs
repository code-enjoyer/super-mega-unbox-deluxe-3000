using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using SuperMegaUnboxDeluxe.Api.Controllers;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;
using Constants = SuperMegaUnboxDeluxe.Api.Constants;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("API")]
public sealed class HostingTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Theory]
    [InlineData("/api/status", "{\"status\":\"ok\"}")]
    [InlineData("/health", "Healthy")]
    public async Task Api_StartupEndpointsWithAvailablePersistence_ShouldReturnExpectedBody(string path, string expectedBody)
    {
        using var client = CreateClient(_factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(Constants.Headers.CorrelationId, "startup-check");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe(expectedBody);
        response.Headers.GetValues(Constants.Headers.CorrelationId).Single().ShouldBe("startup-check");
    }

    [Fact]
    public async Task Api_DatabaseOutage_ShouldKeepStatusAvailableAndRecoverHealth()
    {
        _factory.DatabaseAvailable = false;
        using var client = CreateClient(_factory);
        using var health = await client.GetAsync("/health");
        using var status = await client.GetAsync("/api/status");

        health.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await health.Content.ReadAsStringAsync()).ShouldBe("Unhealthy");
        status.StatusCode.ShouldBe(HttpStatusCode.OK);

        _factory.DatabaseAvailable = true;
        using var recovered = await client.GetAsync("/health");
        recovered.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await recovered.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Api_DisabledDocumentation_ShouldRedirectRootToStatus()
    {
        using var client = CreateClient(_factory);
        using var root = await client.GetAsync("/");
        using var openApi = await client.GetAsync("/openapi/v1.json");

        root.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        root.Headers.Location!.OriginalString.ShouldBe("/api/status");
        openApi.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Api_EnabledDocumentation_ShouldExposeSchemaAndScalar()
    {
        using var factory = new ApiTestFactory(enableOpenApi: true);
        using var client = CreateClient(factory);
        using var root = await client.GetAsync("/");
        using var openApi = await client.GetAsync("/openapi/v1.json");
        using var scalarRedirect = await client.GetAsync("/scalar");
        using var scalar = await client.GetAsync(scalarRedirect.Headers.Location);

        root.Headers.Location!.OriginalString.ShouldBe("/scalar");
        openApi.StatusCode.ShouldBe(HttpStatusCode.OK);
        scalarRedirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        scalar.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/status", HttpStatusCode.OK)]
    [InlineData("/test/failure", HttpStatusCode.InternalServerError)]
    public async Task Api_RequestWithoutCorrelationId_ShouldReceiveOne(string path, HttpStatusCode expectedStatus)
    {
        using var client = CreateClient(_factory);
        using var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(expectedStatus);
        Guid.TryParseExact(response.Headers.GetValues(Constants.Headers.CorrelationId).Single(), "N", out _)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Api_HandledError_ShouldKeepCorrelationAndLogExceptionOnce()
    {
        const string correlationId = "failure-check";
        using var client = CreateClient(_factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/failure");
        request.Headers.Add(Constants.Headers.CorrelationId, correlationId);
        var previousEvents = _factory.Events.Count;

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        response.Headers.GetValues(Constants.Headers.CorrelationId).Single().ShouldBe(correlationId);
        (await response.Content.ReadAsStringAsync()).ShouldContain(correlationId);

        var events = _factory.Events.Skip(previousEvents).ToArray();
        events.Count(logEvent => logEvent.Exception is ApiTestException).ShouldBe(1);
        var requestEvent = events.Single(logEvent =>
            logEvent.Properties.TryGetValue("CorrelationId", out var value) &&
            value is ScalarValue { Value: correlationId });
        requestEvent.Exception.ShouldBeNull();
        requestEvent.Properties["StatusCode"].ShouldBe(new ScalarValue(500));
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Api_CorsPreflight_ShouldAllowOnlyTheConfiguredClient(string origin, bool allowed)
    {
        using var client = CreateClient(_factory);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/items");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBe(allowed);
        if (allowed) response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(origin);
    }

    private static HttpClient CreateClient(WebApplicationFactory<StatusController> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}

public sealed class ApiTestFactory : WebApplicationFactory<StatusController>, ILogEventSink
{
    internal const string FixtureConnectionString = "Host=127.0.0.1;Database=smud_tests;Username=fixture;Password=fixture-only";
    private readonly bool _enableOpenApi;
    private readonly Action<IServiceCollection>? _configure;
    private readonly string? _connectionString;

    public ApiTestFactory(bool enableOpenApi = false, Action<IServiceCollection>? configure = null, string? connectionString = null)
    {
        _enableOpenApi = enableOpenApi;
        _configure = configure;
        _connectionString = connectionString;
    }

    public ConcurrentQueue<LogEvent> Events { get; } = new();
    public bool DatabaseAvailable { get; set; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Never load a developer's User Secrets or require a local database.
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:EnableOpenApi"] = _enableOpenApi.ToString(),
                ["Api:EnableDeveloperExceptionPage"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(FailureController).Assembly);
            // Replace connectivity at the test boundary; the production health
            // check remains registered and can observe outages and recovery.
            if (_connectionString is null) services.AddScoped(_ =>
            {
                var context = Substitute.For<SmudDbContext>(new DbContextOptionsBuilder<SmudDbContext>().Options);
                var database = Substitute.For<DatabaseFacade>(context);
                database.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(DatabaseAvailable));
                context.Database.Returns(database);
                return context;
            });
            _configure?.Invoke(services);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Supply required configuration before the minimal host registers services.
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:SmudDatabase"] = _connectionString ?? FixtureConnectionString,
                ["ConnectionStrings:SmudClient"] = "http://localhost:5173",
                ["Persistence:Password"] = "",
                ["Telemetry:Enabled"] = "false"
            }));
        // Capture logs in memory instead of writing the application's log files.
        builder.UseSerilog((_, configuration) => configuration.WriteTo.Sink(this));
        return base.CreateHost(builder);
    }

    public void Emit(LogEvent logEvent)
    {
        Events.Enqueue(logEvent);
    }
}

// Included only by the test host, never by the running API.
[ApiController]
[Route("test/failure")]
public sealed class FailureController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        throw new ApiTestException();
    }
}

public sealed class ApiTestException : Exception
{
}

// The API uses Serilog's process-wide logger; keep its test hosts sequential.
[CollectionDefinition("API", DisableParallelization = true)]
public sealed class ApiTestCollection
{
}
