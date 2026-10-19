using SuperMegaUnboxDeluxe.Application.Items.Abstractions;
using SuperMegaUnboxDeluxe.Domain.Entities;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence;

internal sealed class ItemRepository(SmudDbContext database) : IItemRepository
{
    public void Add(Item item) => database.Items.Add(item);
}
