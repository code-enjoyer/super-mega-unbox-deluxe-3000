using SuperMegaUnboxDeluxe.Domain.Enums;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public class ItemStat
{
    public ItemStatType Type { get; private set; }
    public string Value { get; private set; } = null!;

    private ItemStat() { }

    public ItemStat(ItemStatType type, string value)
    {
        Type = type;
        Value = value;
    }
}
