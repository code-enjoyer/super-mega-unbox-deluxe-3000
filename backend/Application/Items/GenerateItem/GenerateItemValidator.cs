using System.Linq;
using FluentValidation;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Application.Items.GenerateItem;

public sealed class GenerateItemValidator : AbstractValidator<GenerateItemCommand>
{
    public GenerateItemValidator(IItemCatalog catalog)
    {
        RuleFor(command => command.LootSourceId)
            .Must(source => source is null || catalog.Sources.Any(candidate => candidate.Id == source))
            .WithErrorCode(ItemErrors.UnknownSource.Code)
            .WithMessage(ItemErrors.UnknownSource.Detail!);
    }
}
