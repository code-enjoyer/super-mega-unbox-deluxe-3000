using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>, ISmudDbContextConfiguration
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Rarity).HasColumnName("rarity").HasConversion<string>().IsRequired();
        builder.Property(x => x.ImageKey).HasColumnName("image_key").IsRequired();
        builder.Property(x => x.DateGotten).HasColumnName("date_gotten");
        builder.Property(x => x.Value).HasColumnName("value");
        builder.Property<System.Guid>("BaseId").HasColumnName("base_id");
        builder.HasIndex("BaseId").HasDatabaseName("ix_items_base_id");
        builder.HasOne(x => x.Base).WithMany().HasForeignKey("BaseId").IsRequired().HasConstraintName("fk_items_item_bases_base_id");

        builder.OwnsMany(x => x.Stats, stat =>
        {
            stat.ToTable("item_stats");
            stat.Property<int>("Id").HasColumnName("id");
            stat.Property<System.Guid>("ItemId").HasColumnName("item_id");
            stat.WithOwner().HasForeignKey("ItemId").HasConstraintName("fk_item_stats_items_item_id");
            stat.HasIndex("ItemId").HasDatabaseName("ix_item_stats_item_id");
            stat.HasKey("Id").HasName("pk_item_stats");
            stat.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
            stat.Property(x => x.Value).HasColumnName("value").IsRequired();
        });

        builder.OwnsMany(x => x.Modifiers, modifier =>
        {
            modifier.ToTable("item_modifiers");
            modifier.Property<int>("Id").HasColumnName("id");
            modifier.Property<System.Guid>("ItemId").HasColumnName("item_id");
            modifier.WithOwner().HasForeignKey("ItemId").HasConstraintName("fk_item_modifiers_items_item_id");
            modifier.HasIndex("ItemId").HasDatabaseName("ix_item_modifiers_item_id");
            modifier.HasKey("Id").HasName("pk_item_modifiers");
            modifier.Property(x => x.Name).HasColumnName("name").IsRequired();
            modifier.Property(x => x.Value).HasColumnName("value").IsRequired();
            modifier.Property(x => x.Details)
                .HasColumnName("details")
                .HasConversion(
                    details => JsonSerializer.Serialize(details, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<ModifierDetails>(json, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb");
        });

        builder.Navigation(x => x.Stats).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Modifiers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
