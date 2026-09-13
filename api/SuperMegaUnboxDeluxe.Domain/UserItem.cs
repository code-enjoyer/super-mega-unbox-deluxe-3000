using SuperMegaUnboxDeluxe.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SuperMegaUnboxDeluxe.Domain;

public class UserItem
{
    private List<ItemStat> _stats = new();
    private List<ItemModifier> _modifiers = new();

    public ItemBase Base { get; private set; }
    public string Name { get; private set; }
    public ItemRarity Rarity { get; private set; }
    public string ImageKey { get; private set; }
    public DateTimeOffset DateGotten { get; private set; }
    public float Value { get; private set; }

    public IReadOnlyList<ItemStat> Stats => _stats.AsReadOnly();
    public IReadOnlyList<ItemModifier> Modifiers => _modifiers.AsReadOnly();
}
