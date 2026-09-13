using Microsoft.Extensions.Configuration;
using SuperMegaUnboxDeluxe.Common.Extensions.StringExtensions;
using System;

namespace SuperMegaUnboxDeluxe.Application.Extensions.ConfigurationExtensions;

public static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);

        if (connectionString.IsEmpty())
            throw new InvalidOperationException($"ConnectionStrings:{name} is required.");
        
        return connectionString;
    }
}
