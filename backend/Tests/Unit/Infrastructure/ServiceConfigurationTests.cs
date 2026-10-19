using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;
using SuperMegaUnboxDeluxe.Api.HealthChecks;
using SuperMegaUnboxDeluxe.Api.Settings;
using SuperMegaUnboxDeluxe.Api.Telemetry;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Infrastructure;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;

namespace SuperMegaUnboxDeluxe.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public sealed class ServiceConfigurationTests
{
    [Fact]
    public void Infrastructure_ConnectionString_ShouldRegisterPersistenceAndReadiness()
    {
        const string connectionString = "Host=localhost;Database=smud_dev;Username=smud_user;Password=fixture-only";
        var configuration = Configure(("ConnectionStrings:SmudDatabase", connectionString));
        var services = new ServiceCollection().AddInfrastructure(configuration).AddSmudHealthChecks();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmudDbContext>();

        context.Database.GetConnectionString().ShouldBe(connectionString);
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ShouldBeSameAs(context);
        provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .ShouldContain(registration => registration.Name == HealthCheckNames.SmudDatabase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Infrastructure_MissingConnection_ShouldFailWithoutPrintingConfiguration(string? connectionString)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:SmudDatabase"] = connectionString }).Build();
        var error = Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));

        error.Message.ShouldBe("ConnectionStrings:SmudDatabase is required.");
    }

    [Fact]
    public void Infrastructure_ContainerPassword_ShouldBeEscapedAndRegisterUnitOfWork()
    {
        const string password = "fixture;password='with-special-characters";
        var configuration = Configure(("Persistence:Password", password));
        var services = new ServiceCollection().AddInfrastructure(configuration).AddSmudHealthChecks();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SmudDbContext>();

        new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString()).Password.ShouldBe(password);
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ShouldBeSameAs(context);
        provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .ShouldContain(registration => registration.Name == HealthCheckNames.SmudDatabase);
        // Generate migration SQL without connecting to a database.
        context.GetService<IMigrator>().GenerateScript().ShouldContain("__EFMigrationsHistory");
        context.Model.GetEntityTypes().ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("file:///tmp/collector")]
    [InlineData("http://user:fixture@localhost:4328")]
    [InlineData("http://localhost:4328?token=fixture")]
    public void Telemetry_InvalidEndpoint_ShouldFailWithoutEchoingItsValue(string endpoint)
    {
        var configuration = Configure(("Telemetry:Enabled", "true"), ("Telemetry:Endpoint", endpoint));
        var error = Should.Throw<InvalidOperationException>(() => configuration.GetSettings<TelemetrySettings>().Validate());

        error.Message.ShouldNotContain(endpoint);
    }

    [Fact]
    public void Telemetry_UnsafeKey_ShouldFailWithoutEchoingItsValue()
    {
        const string invalidKey = "fixture,unsafe=header";
        var configuration = Configure(("Telemetry:Enabled", "true"), ("Telemetry:ApiKey", invalidKey));
        Should.Throw<InvalidOperationException>(() => configuration.GetSettings<TelemetrySettings>().Validate()).Message.ShouldNotContain(invalidKey);
    }

    [Fact]
    public void Telemetry_SharedSettings_ShouldBindOptionsConsistently()
    {
        var configuration = Configure(("Telemetry:Enabled", "true"),
            ("Telemetry:Endpoint", "http://localhost:4328/collector"),
            ("Telemetry:ApiKey", "fixture-only-otlp-key-never-real"));
        var settings = configuration.GetSettings<TelemetrySettings>();
        using var provider = new ServiceCollection().AddSettings(configuration).BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TelemetrySettings>>().Value;

        options.Enabled.ShouldBeTrue();
        options.Endpoint.ShouldBe(settings.Endpoint);
        options.ApiKey.ShouldBe(settings.ApiKey);
        options.GetEndpoint().AbsoluteUri.ShouldBe("http://localhost:4328/collector/");
    }

    [Fact]
    public void Telemetry_Disabled_ShouldResolveOptionsWithoutCredentials()
    {
        var configuration = Configure(("Telemetry:Enabled", "false"));
        using var provider = new ServiceCollection().AddSettings(configuration).BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<TelemetrySettings>>().Value;
        settings.Enabled.ShouldBeFalse();
        settings.GetEndpoint().AbsoluteUri.ShouldBe("http://127.0.0.1:4328/");
    }

    [Fact]
    public void Telemetry_InvalidOptions_ShouldFailWithoutEchoingConfiguration()
    {
        const string endpoint = "http://user:fixture@localhost:4328";
        var configuration = Configure(("Telemetry:Enabled", "true"), ("Telemetry:Endpoint", endpoint));
        using var provider = new ServiceCollection().AddSettings(configuration).BuildServiceProvider();

        var error = Should.Throw<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptions<TelemetrySettings>>().Value);
        error.Message.ShouldNotContain(endpoint);
    }

    [Fact]
    public void Telemetry_Disabled_ShouldNotRegisterExportProviders()
    {
        var configuration = Configure(("Telemetry:Enabled", "false"));
        using var provider = new ServiceCollection().AddSmudTelemetry(configuration).BuildServiceProvider();
        provider.GetService<TracerProvider>().ShouldBeNull();
        provider.GetService<MeterProvider>().ShouldBeNull();
    }

    private static IConfiguration Configure(params (string Key, string Value)[] settings)
    {
        var values = new Dictionary<string, string?>();
        foreach (var (key, value) in settings) { values.Add(key, value); }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}

