using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class DeletePreventionGuideRequestValidator : AbstractValidator<DeletePreventionGuideRequest>
{
    public DeletePreventionGuideRequestValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty()
            .MaximumLength(2_000);
    }
}
