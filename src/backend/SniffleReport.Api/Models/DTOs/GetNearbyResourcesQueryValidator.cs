using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetNearbyResourcesQueryValidator : AbstractValidator<GetNearbyResourcesQuery>
{
    public GetNearbyResourcesQueryValidator()
    {
        RuleFor(x => x.Lat)
            .NotNull()
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Lng)
            .NotNull()
            .InclusiveBetween(-180, 180);

        RuleFor(x => x.Radius)
            .GreaterThan(0)
            .LessThanOrEqualTo(50);

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
