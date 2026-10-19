using System;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Infrastructure.Items;

internal sealed class SystemRandomSource : IRandomSource
{
    public int NextInclusive(int min, int max) => checked((int)Random.Shared.NextInt64(min, (long)max + 1));
}
