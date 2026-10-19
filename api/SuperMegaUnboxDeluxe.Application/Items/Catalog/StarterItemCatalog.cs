using SuperMegaUnboxDeluxe.Domain.Enums;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;
using System;
using System.Collections.Immutable;

namespace SuperMegaUnboxDeluxe.Application.Items.Catalog;

public sealed class StarterItemCatalog : IItemCatalog
{
    // Persisted identities: never regenerate or reuse these IDs for different bases.
    public static readonly Guid FingerGunsId = Guid.Parse("fa000000-0000-0000-0000-000000000001");
    public static readonly Guid SalamanderId = Guid.Parse("fa000000-0000-0000-0000-000000000002");
    public static readonly Guid ShieldId = Guid.Parse("fa000000-0000-0000-0000-000000000003");
    public static readonly Guid CoatId = Guid.Parse("fa000000-0000-0000-0000-000000000004");

    public string DefaultSourceId => "suspicious-lost-property";
    public ImmutableArray<BaseDefinition> Bases { get; }
    public ImmutableArray<AffixDefinition> Affixes { get; }
    public ImmutableArray<UniqueDefinition> Uniques { get; }
    public ImmutableArray<LootSourceDefinition> Sources { get; }

    public StarterItemCatalog()
    {
        ImmutableArray<Guid> weapons = [FingerGunsId, SalamanderId];
        ImmutableArray<Guid> armour = [ShieldId, CoatId];
        ImmutableArray<Guid> all = [FingerGunsId, SalamanderId, ShieldId, CoatId];
        Bases =
        [
            new(FingerGunsId, "Finger Guns", "finger-guns", Stats(ItemStatType.Damage, 3, 8)),
            new(SalamanderId, "Petrified Salamander", "petrified-salamander", Stats(ItemStatType.Damage, 6, 12)),
            new(ShieldId, "Turtle-Shell Shield", "turtle-shell-shield", Stats(ItemStatType.Defense, 5, 10)),
            new(CoatId, "Suspicious Trench Coat", "suspicious-trench-coat", Stats(ItemStatType.Defense, 2, 7))
        ];
        Affixes =
        [
            new("suspiciously-moist", "Suspiciously Moist", AffixSlot.Prefix, all,
                new(ItemStatType.Durability, new(5, 15)), null),
            new("aggressively-pointy", "Aggressively Pointy", AffixSlot.Prefix, weapons,
                new(ItemStatType.Damage, new(2, 5)), null),
            new("administrative-protection", "of Administrative Protection", AffixSlot.Suffix, armour,
                new(ItemStatType.Defense, new(2, 5)), null),
            new("existential-dread", "of Existential Dread", AffixSlot.Suffix, weapons, null,
                new("existential-dread", EffectTrigger.OnHit, new(5, 15), new(1, 3), null,
                    "On hit: {0}% chance to inflict Existential Dread for {1} seconds.")),
            new("emergency-geese", "of Emergency Geese", AffixSlot.Suffix, armour, null,
                new("emergency-geese", EffectTrigger.OnBeingHit, new(5, 15), null, new(1, 1),
                    "On being hit: {0}% chance to summon {2} advisory goose. Its advice is mostly honking."))
        ];
        Uniques =
        [
            new("sir-newt-on", SalamanderId, "Sir Newt-on, the Unreasonably Rigid", "sir-newt-on",
                [new(ItemStatType.Damage, new(20, 30)), new(ItemStatType.Durability, new(80, 100))],
                new("formal-challenge", "A Formal Challenge to Gravity", AffixSlot.Signature, [SalamanderId], null,
                    new("challenge-gravity", EffectTrigger.OnHit, new(10, 10), null, null,
                        "On hit: {0}% chance to issue a formal challenge to gravity.")), 50)
        ];
        Sources =
        [
            new(DefaultSourceId, "Suspicious Lost Property", "Unclaimed weapons, questionable armour, and absolutely no receipts.",
                all, ["sir-newt-on"], Weights(55, 25, 12, 5, 2, 1)),
            new("definitely-legal-arms-dealer", "Definitely Legal Arms Dealer", "Weapons only. The finger guns are technically arms.",
                weapons, ["sir-newt-on"], Weights(35, 30, 20, 10, 4, 1))
        ];
        ItemCatalogValidator.Validate(this);
    }

    private static ImmutableArray<StatDefinition> Stats(ItemStatType type, int min, int max) =>
        [new(type, new(min, max)), new(ItemStatType.Durability, new(40, 100))];

    private static ImmutableArray<RarityWeight> Weights(int common, int uncommon, int rare, int epic, int legendary, int unique) =>
    [
        new(ItemRarity.Common, common), new(ItemRarity.Uncommon, uncommon), new(ItemRarity.Rare, rare),
        new(ItemRarity.Epic, epic), new(ItemRarity.Legendary, legendary), new(ItemRarity.Unique, unique)
    ];
}
