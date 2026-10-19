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
        public ModifierDetails? Details { get; init; }
    }

    public sealed record ModifierDetails(string DefinitionId, string Role, StatBonus? Bonus, SpecialEffect? Effect);
    public sealed record StatBonus(string Type, int Amount);
    public sealed record SpecialEffect(string Id, string Trigger, int ChancePercent, int? DurationSeconds, int? Count);
}
