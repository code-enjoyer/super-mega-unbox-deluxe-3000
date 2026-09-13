using Microsoft.EntityFrameworkCore;
using SuperMegaUnboxDeluxe.Application;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence;

public class SmudDbContext : DbContext, IUnitOfWork
{
    public SmudDbContext(DbContextOptions<SmudDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmudDbContext).Assembly,
            type => type.IsAssignableTo(typeof(ISmudDbContextConfiguration)));
    }
}
