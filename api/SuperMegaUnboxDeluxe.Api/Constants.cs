namespace SuperMegaUnboxDeluxe.Api;

public static class Constants
{
    public static class Actions
    {
        public const string GetItemDetails = "GetItemDetails";
        public const string GenerateItem = "GenerateItem";
    }

    public static class EnvironmentVariables
    {
        public const string AspNetEnvironment = "ASPNETCORE_ENVIRONMENT";
    }

    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-ID";
    }
}
