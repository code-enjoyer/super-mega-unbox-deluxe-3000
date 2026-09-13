using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SuperMegaUnboxDeluxe.Common.Extensions.EnumerableExtensions;

public static class EnumerableExtensions
{
    public static bool IsEmpty([NotNullWhen(returnValue: false)] this IEnumerable<object>? enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }

    public static bool IsNotEmpty([NotNullWhen(returnValue: true)] this IEnumerable<object>? enumerable)
    {
        return !enumerable.IsEmpty();
    }

    public static bool TryGetValue<TValue, TTarget>(this IEnumerable<KeyValuePair<string, TValue>> enumerable, string key,
        [NotNullWhen(returnValue: true)] out TTarget? value)
    {
        value = default;
        
        if (enumerable == null)
            return false;

        var pair = enumerable.FirstOrDefault(x => x.Key == key);
        
        if (pair.Equals(default(KeyValuePair<string, TValue>)))
            return false;

        if (pair.Value is TTarget targetValue)
        {
            value = targetValue;

            return true;
        }

        return false;
    }
}
