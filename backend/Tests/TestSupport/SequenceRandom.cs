using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Tests.TestSupport;

internal sealed class SequenceRandom(params int[] values) : IRandomSource
{
    private readonly Queue<int> _values = new(values);
    public int NextInclusive(int min, int max)
    {
        var value = _values.TryDequeue(out var next) ? next : min;
        value.ShouldBeInRange(min, max);
        return value;
    }
}
