using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAdminResourcesQueryValidator : AbstractValidator<GetAdminResourcesQuery>
{
    public GetAdminResourcesQueryValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
