using SuperMegaUnboxDeluxe.Common.Results;

namespace SuperMegaUnboxDeluxe.Application.Items;

public static class ItemErrors
{
    public static readonly Error UnknownSource = new(ErrorKind.Validation, "items.unknown_source", "Unknown loot source",
        "Choose a lootSourceId returned by GET /loot-sources.");
    public static readonly Error NotFound = new(ErrorKind.NotFound, "items.not_found", "Item not found");
}
