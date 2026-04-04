using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class UpdateAlertRequestValidator : AbstractValidator<UpdateAlertRequest>
{
    public UpdateAlertRequestValidator()
    {
        RuleFor(x => x.RegionId)
            .NotEmpty();

        RuleFor(x => x.Disease)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Summary)
            .NotEmpty()
            .MaximumLength(2_000);

        RuleFor(x => x.CaseCount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.SourceAttribution)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.SourceDate)
            .NotEqual(default(DateTime));
    }
}
