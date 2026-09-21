using Microsoft.EntityFrameworkCore;
using SuperMegaUnboxDeluxe.Application;
using SuperMegaUnboxDeluxe.Domain.Entities;

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
    }
}
