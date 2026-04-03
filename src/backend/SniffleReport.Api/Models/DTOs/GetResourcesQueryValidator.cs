using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetResourcesQueryValidator : AbstractValidator<GetResourcesQuery>
{
    public GetResourcesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
