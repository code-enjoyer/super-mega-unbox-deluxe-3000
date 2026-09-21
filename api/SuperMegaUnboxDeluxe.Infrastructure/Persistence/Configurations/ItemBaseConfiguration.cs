using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMegaUnboxDeluxe.Domain.Entities;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence.Configurations;

internal sealed class ItemBaseConfiguration : IEntityTypeConfiguration<ItemBase>, ISmudDbContextConfiguration
{
    public void Configure(EntityTypeBuilder<ItemBase> builder)
    {
        builder.ToTable("item_bases");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Name).IsRequired();
    }
}
