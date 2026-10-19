using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SuperMegaUnboxDeluxe.Domain.Entities;
using SuperMegaUnboxDeluxe.Domain.Events;
using SuperMegaUnboxDeluxe.Infrastructure.Persistence;

namespace SuperMegaUnboxDeluxe.Tests.Unit.Infrastructure.Persistence;

[Trait("Category", "Unit")]
public sealed class SmudDbContextTests
{
    [Fact]
    public void SmudDbContext_ExistingMappings_ShouldMatchMigrationSnapshot()
    {
        using var db = new SmudDbContext(Options());

        (db.Database.HasPendingModelChanges()).ShouldBeFalse();
    }

    [Fact]
    public void SmudDbContext_EntityWithoutIndividualConfiguration_ShouldApplySharedMapping()
    {
        using var db = new ProbeDbContext(Options());

        foreach (var clrType in new[] { typeof(Item), typeof(ItemBase), typeof(ProbeEntity) })
        {
            var entityType = db.Model.FindEntityType(clrType);
            entityType.ShouldNotBeNull();
            var key = (entityType.FindPrimaryKey()!.Properties).ShouldHaveSingleItem();
            key.Name.ShouldBe(nameof(Entity.Id));
            (entityType.FindProperty(nameof(Entity.DomainEvents))).ShouldBeNull();
            (entityType.FindNavigation(nameof(Entity.DomainEvents))).ShouldBeNull();
        }

        (db.Model.GetEntityTypes()).ShouldNotContain(entityType => typeof(IDomainEvent).IsAssignableFrom(entityType.ClrType));
    }

    [Fact]
    public void SmudDbContext_RelationalMappings_ShouldUseSnakeCaseNames()
    {
        using var db = new SmudDbContext(Options());
        foreach (var table in db.Model.GetRelationalModel().Tables)
        {
            table.Name.ShouldMatch("^[a-z][a-z0-9_]*$");
            foreach (var column in table.Columns) column.Name.ShouldMatch("^[a-z][a-z0-9_]*$");
            table.PrimaryKey!.Name.ShouldMatch("^pk_[a-z0-9_]+$");
            foreach (var index in table.Indexes) index.Name.ShouldMatch("^ix_[a-z0-9_]+$");
            foreach (var foreignKey in table.ForeignKeyConstraints) foreignKey.Name.ShouldMatch("^fk_[a-z0-9_]+$");
        }
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
