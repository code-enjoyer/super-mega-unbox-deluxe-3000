using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using System.Linq;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence.Configurations;

internal sealed class ItemBaseConfiguration : IEntityTypeConfiguration<ItemBase>, ISmudDbContextConfiguration
{
    public void Configure(EntityTypeBuilder<ItemBase> builder)
    {
        builder.ToTable("item_bases");
        builder.Property(x => x.Name).IsRequired();
        builder.HasData(new StarterItemCatalog().Bases.Select(x => new { x.Id, x.Name }));
    }
}
