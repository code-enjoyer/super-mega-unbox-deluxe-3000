using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMegaUnboxDeluxe.Application.Items.Catalog;
using SuperMegaUnboxDeluxe.Domain.Entities;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence.Configurations;

internal sealed class ItemBaseConfiguration : IEntityTypeConfiguration<ItemBase>, ISmudDbContextConfiguration
{
    public void Configure(EntityTypeBuilder<ItemBase> builder)
    {
        builder.ToTable("item_bases");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.HasData(new StarterItemCatalog().Bases.Select(x => new { x.Id, x.Name }));
    }
}
