using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using SuperMegaUnboxDeluxe.Api.Controllers;
using SuperMegaUnboxDeluxe.Api.Logging;
using SuperMegaUnboxDeluxe.Api.Telemetry;

namespace SuperMegaUnboxDeluxe.Tests.Integration.Api;

[Trait("Category", "Integration")]
[Collection("API")]
public sealed class TelemetryTests
{
    [Fact]
    public async Task Telemetry_StatusRequest_ShouldExportAuthenticatedCorrelatedSignals()
    {
        // An actual HTTP OTLP receiver, independent of Docker and developer secrets.
        var received = new ConcurrentQueue<(string Path, byte[] Body)>();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var receiver = builder.Build();
        receiver.MapPost("/v1/{signal}", async context =>
        {
            context.Request.Headers["x-otlp-api-key"].ToString().ShouldBe(OtlpApiFactory.FixtureKey);
            context.Request.ContentType.ShouldBe("application/x-protobuf");
            using var body = new MemoryStream();
            await context.Request.Body.CopyToAsync(body);
            received.Enqueue((context.Request.Path.Value!, body.ToArray()));
            context.Response.ContentType = "application/x-protobuf";
        });
        await receiver.StartAsync();

        await using var factory = new OtlpApiFactory(receiver.Urls.Single());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync("/api/status");
        response.EnsureSuccessStatusCode();
        await factory.RequestCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        factory.Services.GetRequiredService<TracerProvider>().ForceFlush().ShouldBeTrue();
        factory.Services.GetRequiredService<MeterProvider>().ForceFlush().ShouldBeTrue();
        await factory.Logger!.DisposeAsync();

        var requestLog = factory.Events.Single(logEvent =>
            logEvent.MessageTemplate.Text.StartsWith("HTTP ", StringComparison.Ordinal) &&
            logEvent.Properties.TryGetValue("RequestPath", out var path) && path.Equals(new ScalarValue("/api/status")));
        requestLog.TraceId.ShouldNotBeNull();
        requestLog.SpanId.ShouldNotBeNull();
        var traceId = Convert.FromHexString(requestLog.TraceId!.Value.ToHexString());
        foreach (var signal in new[] { "logs", "traces", "metrics" })
        {
            var bodies = received.Where(item => item.Path == "/v1/" + signal).Select(item => item.Body).ToArray();
            bodies.ShouldNotBeEmpty("Missing exported " + signal);
            bodies.ShouldContain(body => System.Text.Encoding.UTF8.GetString(body).Contains("SuperMegaUnboxDeluxe.TelemetryTest", StringComparison.Ordinal));
            if (signal != "metrics")
            {
                bodies.ShouldContain(body => ContainsTraceId(body, traceId));
            }
        }
    }

    private static bool ContainsTraceId(byte[] body, byte[] traceId) => body.AsSpan().IndexOf(traceId) >= 0;
}

public sealed class OtlpApiFactory(string endpoint) : WebApplicationFactory<StatusController>, ILogEventSink
{
    public const string FixtureKey = "fixture-only-otlp-key-never-real";
    public ConcurrentQueue<LogEvent> Events { get; } = new();
    public TaskCompletionSource RequestCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Logger? Logger { get; private set; }
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true",
            ["Telemetry:Endpoint"] = endpoint,
            ["Telemetry:ApiKey"] = FixtureKey,
            ["Application:Name"] = "SuperMegaUnboxDeluxe.TelemetryTest"
        }).Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddConfiguration(_configuration));
        // The receiver shares this process; do not record its inbound exporter requests.
        builder.ConfigureTestServices(services =>
        {
            services.AddOpenTelemetry().WithTracing(tracing =>
                tracing.AddProcessor(new RequestCompletionProcessor(RequestCompleted)));
            services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
                options.Filter = context => !context.Request.Path.StartsWithSegments("/v1"));
            // Keep request logs on this host's owned logger when other test hosts
            // close the process-wide bootstrap logger during shutdown.
            services.Configure<RequestLoggingOptions>(options => options.Logger = Logger);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:SmudDatabase"] = ApiTestFactory.FixtureConnectionString,
                ["ConnectionStrings:SmudClient"] = "http://localhost:5173",
                ["Persistence:Password"] = ""
            }).AddConfiguration(_configuration));
        Logger = new LoggerConfiguration().ConfigureLogger(_configuration).WriteTo.Sink(this).CreateLogger();
        builder.UseSerilog(Logger, dispose: true);
        return base.CreateHost(builder);
    }

    public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
}

internal sealed class RequestCompletionProcessor(TaskCompletionSource completed) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.GetTagItem("http.route") as string == "api/status")
        {
            completed.TrySetResult();
        }
    }
}
