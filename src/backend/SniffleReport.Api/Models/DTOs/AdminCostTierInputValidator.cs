using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminCostTierInputValidator : AbstractValidator<AdminCostTierInput>
{
    public AdminCostTierInputValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Notes)
            .MaximumLength(1_000);
    }
}
