using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetRegionsQueryValidator : AbstractValidator<GetRegionsQuery>
{
    public GetRegionsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
