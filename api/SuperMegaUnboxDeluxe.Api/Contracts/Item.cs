using System;

namespace SuperMegaUnboxDeluxe.Api.Contracts;

public class Item
{
    public Guid Id { get; set; }
    public required string ItemBase { get; init; }
    public required string Name { get; init; }
    public required ItemStat[] Stats { get; init; }
    public required ItemModifier[] Modifiers { get; init; }
    public required string Rarity { get; init; }
    public required string ImageKey { get; init; }
    public DateTimeOffset DateGotten { get; init; }
    public float Value { get; init; }

    public class ItemStat
    {
        public required string Name { get; init; }
        public required string Value { get; init; }
    }

    public class ItemModifier
    {
        public required string Name { get; init; }
        public required string Value { get; init; }
    }
}
