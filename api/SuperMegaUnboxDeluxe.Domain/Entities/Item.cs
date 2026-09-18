using System;
using System.Collections.Generic;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public class Item : Entity
{
    private readonly List<ItemStat> _stats = new();
    private readonly List<ItemModifier> _modifiers = new();

    public required ItemBase Base { get; init; }
    public required string Name { get; init; }
    public required string Rarity { get; init; }
    public required string ImageKey { get; init; }
    public DateTimeOffset DateGotten { get; init; }
    public float Value { get; init; }

    public IReadOnlyList<ItemStat> Stats => _stats.AsReadOnly();
    public IReadOnlyList<ItemModifier> Modifiers => _modifiers.AsReadOnly();
}
