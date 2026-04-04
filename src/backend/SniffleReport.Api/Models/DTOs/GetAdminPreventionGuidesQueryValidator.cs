using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAdminPreventionGuidesQueryValidator : AbstractValidator<GetAdminPreventionGuidesQuery>
{
    public GetAdminPreventionGuidesQueryValidator()
    {
        RuleFor(x => x.Disease)
            .MaximumLength(120);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
