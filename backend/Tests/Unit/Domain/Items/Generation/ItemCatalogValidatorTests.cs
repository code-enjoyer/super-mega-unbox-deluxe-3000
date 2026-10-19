using System;
using System.Collections.Immutable;
using System.Linq;
using Shouldly;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Enums;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Tests.Unit.Domain.Items.Generation;

[Trait("Category", "Unit")]
public sealed class ItemCatalogValidatorTests
{
    [Fact]
    public void ItemCatalogValidator_InvalidCatalogContent_ShouldThrow()
    {
        var catalog = new TestCatalog(new StarterItemCatalog());
        Should.Throw<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { DefaultSourceId = "missing" }));
        Should.Throw<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { Bases = [catalog.Bases[0], catalog.Bases[0]] }));
        Should.Throw<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { Affixes = [] }));
        Should.Throw<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with { Uniques = [] }));
        Should.Throw<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with
        {
            Bases = catalog.Bases.SetItem(0, catalog.Bases[0] with { Stats = [new(ItemStatType.Damage, new(10, 1))] })
        }));
        Should.Throw<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with
        {
            Sources = [catalog.Sources[0] with { Rarities = [new(ItemRarity.Common, 0)] }]
        }));
        Should.Throw<InvalidOperationException>(() => ItemCatalogValidator.Validate(catalog with
        {
            Affixes = catalog.Affixes.SetItem(0, catalog.Affixes[0] with { CompatibleBases = [Guid.NewGuid()] })
        }));
    }

    private sealed record TestCatalog : IItemCatalog
    {
        public string DefaultSourceId { get; init; }
        public ImmutableArray<BaseDefinition> Bases { get; init; }
        public ImmutableArray<AffixDefinition> Affixes { get; init; }
        public ImmutableArray<UniqueDefinition> Uniques { get; init; }
        public ImmutableArray<LootSourceDefinition> Sources { get; init; }

        public TestCatalog(IItemCatalog original)
        {
            DefaultSourceId = original.DefaultSourceId;
            Bases = original.Bases;
            Affixes = original.Affixes;
            Uniques = original.Uniques;
            Sources = original.Sources;
        }
    }
}
