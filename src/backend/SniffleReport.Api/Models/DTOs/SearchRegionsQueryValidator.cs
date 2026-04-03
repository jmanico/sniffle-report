using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class SearchRegionsQueryValidator : AbstractValidator<SearchRegionsQuery>
{
    public SearchRegionsQueryValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
