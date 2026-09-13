namespace SuperMegaUnboxDeluxe.Api.Settings;

public class ApplicationSettings : ISettings
{
    public static string SettingsName => "Application";

    public required string Name { get; init; }
}
