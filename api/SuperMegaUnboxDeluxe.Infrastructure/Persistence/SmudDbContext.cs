using Microsoft.EntityFrameworkCore;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Domain.Entities;
using System.Linq;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence;

public class SmudDbContext : DbContext, IUnitOfWork
{
    internal DbSet<Item> Items => Set<Item>();

    public SmudDbContext(DbContextOptions<SmudDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmudDbContext).Assembly,
            type => type.IsAssignableTo(typeof(ISmudDbContextConfiguration)));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(type => type.ClrType.IsAssignableTo(typeof(Entity))).ToArray())
        {
            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.HasKey(nameof(Entity.Id));
            builder.Ignore(nameof(Entity.DomainEvents));
        }
    }
}
