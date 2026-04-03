using FluentValidation;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAlertsQueryValidator : AbstractValidator<GetAlertsQuery>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "sourceDate", "caseCount"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public GetAlertsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Disease)
            .MaximumLength(120);

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom!.Value)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);

        RuleFor(x => x.Status)
            .Must(status => status is null || status == AlertStatus.Published)
            .WithMessage("Only published alerts are available on public endpoints.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null || AllowedSortFields.Contains(sortBy))
            .WithMessage("SortBy must be one of: createdAt, sourceDate, caseCount.");

        RuleFor(x => x.SortDirection)
            .Must(direction => direction is null || AllowedSortDirections.Contains(direction.ToLowerInvariant()))
            .WithMessage("SortDirection must be one of: asc, desc.");
    }
}
