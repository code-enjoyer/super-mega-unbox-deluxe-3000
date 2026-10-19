using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SuperMegaUnboxDeluxe.Application.Items;
using SuperMegaUnboxDeluxe.Application.Items.Abstractions;

namespace SuperMegaUnboxDeluxe.Infrastructure.Persistence;

internal sealed class ItemQueries(SmudDbContext database) : IItemQueries
{
    public async Task<ItemDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await database.Items.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                ItemBase = item.Base.Name,
                item.Name,
                item.Rarity,
                item.ImageKey,
                item.DateGotten,
                item.Value,
                Stats = item.Stats.Select(stat => new { stat.Type, stat.Value }).ToArray(),
                Modifiers = item.Modifiers.Select(modifier => new { modifier.Name, modifier.Value, modifier.Details }).ToArray()
            }).SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : new ItemDetails(row.Id, row.ItemBase, row.Name,
            row.Stats.Select(stat => new ItemStatDetails(stat.Type.ToString(), stat.Value)).ToArray(),
            row.Modifiers.Select(modifier => new ItemModifierDetails(modifier.Name, modifier.Value, ItemDetails.MapDetails(modifier.Details))).ToArray(),
            row.Rarity.ToString(), row.ImageKey, row.DateGotten, row.Value);
    }
}
