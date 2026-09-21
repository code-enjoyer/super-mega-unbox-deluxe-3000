using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMegaUnboxDeluxe.Domain.Entities;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>, ISmudDbContextConfiguration
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Rarity).HasConversion<string>().IsRequired();
        builder.Property(x => x.ImageKey).IsRequired();
        builder.HasOne(x => x.Base).WithMany().IsRequired();

        builder.OwnsMany(x => x.Stats, stat =>
        {
            stat.ToTable("item_stats");
            stat.Property<int>("Id");
            stat.HasKey("Id");
            stat.Property(x => x.Type).HasConversion<string>().IsRequired();
            stat.Property(x => x.Value).IsRequired();
        });

        builder.OwnsMany(x => x.Modifiers, modifier =>
        {
            modifier.ToTable("item_modifiers");
            modifier.Property<int>("Id");
            modifier.HasKey("Id");
            modifier.Property(x => x.Name).IsRequired();
            modifier.Property(x => x.Value).IsRequired();
        });

        builder.Navigation(x => x.Stats).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Modifiers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
