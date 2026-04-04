using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class DeleteAlertRequestValidator : AbstractValidator<DeleteAlertRequest>
{
    public DeleteAlertRequestValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty()
            .MaximumLength(2_000);
    }
}
