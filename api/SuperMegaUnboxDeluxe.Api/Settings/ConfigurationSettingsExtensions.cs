using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SuperMegaUnboxDeluxe.Api.Settings;

public static class ConfigurationSettingsExtensions
{
    public static TSettings GetSettings<TSettings>(this IConfiguration configuration) where TSettings : class, ISettings
    {
        return configuration.GetRequiredSection(TSettings.SettingsName).Get<TSettings>() ??
            throw new InvalidOperationException($"Settings for {TSettings.SettingsName} are not configured.");
    }

    public static TProperty GetValue<TSettings, TProperty>(this IConfiguration configuration, Expression<Func<TSettings, TProperty>> expression)
        where TSettings : class, ISettings
    {
        var propertyPath = GetConfigurationPathForProperty(expression);

        return configuration.GetRequiredSection(TSettings.SettingsName).GetValue<TProperty>(propertyPath) ??
            throw new InvalidOperationException($"Value for {TSettings.SettingsName}:{propertyPath} is not configured.");
    }

    private static string GetConfigurationPathForProperty<TSettings, TProperty>(Expression<Func<TSettings, TProperty>> expression)
        where TSettings : class, ISettings
    {
        var current = expression.Body;

        // Unwrap conversions (e.g. when value types are boxed)
        if (current is UnaryExpression unary && unary.Operand != null)
        {
            current = unary.Operand;
        }

        var parts = new List<string>();

        // Walk the MemberExpression chain to collect all member names (supports nested properties)
        while (current is MemberExpression memberExpression)
        {
            parts.Add(memberExpression.Member.Name);
            current = memberExpression.Expression!;
        }

        if (parts.Count == 0)
        {
            throw new InvalidOperationException("Invalid expression type. Only property access expressions are supported.");
        }

        // We collected from leaf to root, so reverse to get root to leaf
        parts.Reverse();

        return string.Join(":", parts);
    }
}
