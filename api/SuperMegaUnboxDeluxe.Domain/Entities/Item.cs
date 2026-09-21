using System;
using System.Collections.Generic;
using SuperMegaUnboxDeluxe.Domain.Enums;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public class Item : Entity
{
    private readonly List<ItemStat> _stats = new();
    private readonly List<ItemModifier> _modifiers = new();

    public ItemBase Base { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ItemRarity Rarity { get; private set; }
    public string ImageKey { get; private set; } = null!;
    public DateTimeOffset DateGotten { get; private set; }
    public float Value { get; private set; }

    public IReadOnlyList<ItemStat> Stats => _stats.AsReadOnly();
    public IReadOnlyList<ItemModifier> Modifiers => _modifiers.AsReadOnly();

    private Item() { }

    public Item(
        ItemBase itemBase,
        string name,
        ItemRarity rarity,
        string imageKey,
        DateTimeOffset dateGotten,
        float value,
        IEnumerable<ItemStat> stats,
        IEnumerable<ItemModifier> modifiers)
    {
        Base = itemBase;
        Name = name;
        Rarity = rarity;
        ImageKey = imageKey;
        DateGotten = dateGotten;
        Value = value;
        _stats.AddRange(stats);
        _modifiers.AddRange(modifiers);
    }
}
