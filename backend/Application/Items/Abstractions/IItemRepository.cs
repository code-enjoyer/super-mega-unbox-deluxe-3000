using SuperMegaUnboxDeluxe.Domain.Entities;

namespace SuperMegaUnboxDeluxe.Application.Items.Abstractions;

public interface IItemRepository
{
    void Add(Item item);
}
