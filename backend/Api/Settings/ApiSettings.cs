namespace SuperMegaUnboxDeluxe.Api.Settings;

/// <summary>
/// Configuration for API and framework behavior.
/// </summary>
public class ApiSettings : ISettings
{
    public static string SettingsName => "Api";

    /// <summary>
    /// Enable OpenAPI/Swagger UI and documentation endpoints.
    /// </summary>
    public required bool EnableOpenApi { get; init; }

    /// <summary>
    /// Enable the developer exception page with detailed error information.
    /// </summary>
    public required bool EnableDeveloperExceptionPage { get; init; }
}
