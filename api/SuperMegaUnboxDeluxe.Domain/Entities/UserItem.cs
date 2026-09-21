namespace SuperMegaUnboxDeluxe.Domain.Entities;

public class UserItem : Entity
{
    public required User User { get; init; }
    public required Item Item { get; init; }
}
