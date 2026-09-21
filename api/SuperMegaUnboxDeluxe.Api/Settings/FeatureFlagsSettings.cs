namespace SuperMegaUnboxDeluxe.Api.Settings;

/// <summary>
/// Configuration for application feature flags that control feature availability and behavior.
/// </summary>
public class FeatureFlags : ISettings
{
    public static string SettingsName => "FeatureFlags";

    // Add domain-specific features here, for example:
    // public required bool EnablePremiumFeatures { get; init; }
    // public required bool EnableNewItemSystem { get; init; }
    // public required bool EnableBetaFeatures { get; init; }
}
