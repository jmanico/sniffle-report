using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAdminNewsItemsQueryValidator : AbstractValidator<GetAdminNewsItemsQuery>
{
    public GetAdminNewsItemsQueryValidator()
    {
        RuleFor(x => x.Headline)
            .MaximumLength(300);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
