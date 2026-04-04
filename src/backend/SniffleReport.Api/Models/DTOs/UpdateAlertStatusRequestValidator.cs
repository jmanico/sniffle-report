using FluentValidation;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class UpdateAlertStatusRequestValidator : AbstractValidator<UpdateAlertStatusRequest>
{
    public UpdateAlertStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Justification)
            .NotEmpty()
            .MaximumLength(2_000)
            .When(x => x.Status is AlertStatus.Archived or AlertStatus.Draft);
    }
}
