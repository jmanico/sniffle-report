using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class CreatePreventionGuideRequestValidator : AbstractValidator<CreatePreventionGuideRequest>
{
    public CreatePreventionGuideRequestValidator()
    {
        RuleFor(x => x.RegionId)
            .NotEmpty();

        RuleFor(x => x.Disease)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(10_000);

        RuleForEach(x => x.CostTiers)
            .SetValidator(new AdminCostTierInputValidator());
    }
}
