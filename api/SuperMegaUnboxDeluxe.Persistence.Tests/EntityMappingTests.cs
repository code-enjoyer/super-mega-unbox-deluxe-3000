using Microsoft.EntityFrameworkCore;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Events;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;

namespace SuperMegaUnboxDeluxe.Persistence.Tests;

public sealed class EntityMappingTests
{
    [Fact]
    public void Shared_mapping_preserves_the_existing_database_schema()
    {
        using var db = new SmudDbContext(Options());

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Shared_mapping_applies_to_new_entities_without_individual_configuration()
    {
        using var db = new ProbeDbContext(Options());

        foreach (var clrType in new[] { typeof(Item), typeof(ItemBase), typeof(ProbeEntity) })
        {
            var entityType = db.Model.FindEntityType(clrType);
            Assert.NotNull(entityType);
            var key = Assert.Single(entityType.FindPrimaryKey()!.Properties);
            Assert.Equal(nameof(Entity.Id), key.Name);
            Assert.Null(entityType.FindProperty(nameof(Entity.DomainEvents)));
            Assert.Null(entityType.FindNavigation(nameof(Entity.DomainEvents)));
        }

        Assert.DoesNotContain(db.Model.GetEntityTypes(),
            entityType => typeof(IDomainEvent).IsAssignableFrom(entityType.ClrType));
    }

    // Model validation uses the real provider but never opens a connection.
    private static DbContextOptions<SmudDbContext> Options() =>
        new DbContextOptionsBuilder<SmudDbContext>()
            .UseNpgsql("Host=localhost;Database=unused_model_validation")
            .Options;

    private sealed class ProbeEntity : Entity;

    private sealed class ProbeDbContext(DbContextOptions<SmudDbContext> options) : SmudDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProbeEntity>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
