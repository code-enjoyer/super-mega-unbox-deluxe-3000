using System.Diagnostics.CodeAnalysis;

namespace SuperMegaUnboxDeluxe.Common.Extensions.StringExtensions;

public static class StringExtensions
{
    public static bool IsEmpty([NotNullWhen(returnValue: false)] this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    public static bool IsNotEmpty([NotNullWhen(returnValue: true)] this string? str)
    {
        return !string.IsNullOrWhiteSpace(str);
    }
}
