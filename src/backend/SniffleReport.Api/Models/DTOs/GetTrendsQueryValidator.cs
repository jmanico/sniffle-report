using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetTrendsQueryValidator : AbstractValidator<GetTrendsQuery>
{
    public GetTrendsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Disease)
            .MaximumLength(120);

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom!.Value)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);

        RuleFor(x => x)
            .Must(query => !query.DateFrom.HasValue
                || !query.DateTo.HasValue
                || query.DateTo.Value <= query.DateFrom.Value.AddYears(1))
            .WithMessage("Date range cannot exceed 1 year.");
    }
}
