using System;
using System.Text.RegularExpressions;

namespace SuperMegaUnboxDeluxe.Api.Settings;

public sealed class TelemetrySettings : ISettings
{
    public static string SettingsName => "Telemetry";

    public bool Enabled { get; init; }
    public string Endpoint { get; init; } = "http://127.0.0.1:4328/";
    public string ApiKey { get; init; } = "";

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        _ = GetEndpoint();

        if (!Regex.IsMatch(ApiKey, "\\A[A-Za-z0-9_-]{22,}\\z"))
        {
            throw new InvalidOperationException("Set Telemetry:ApiKey in User Secrets to the dashboard OTLP key (at least 22 URL-safe characters).");
        }
    }

    public Uri GetEndpoint()
    {
        // Bind as text so malformed URLs cannot leak credentials in binder errors.
        if (!Uri.TryCreate(Endpoint.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException("Telemetry:Endpoint must be an HTTP(S) base URL without credentials, query, or fragment.");
        }

        return uri;
    }
}
