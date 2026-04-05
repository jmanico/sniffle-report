using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetNewsQueryValidator : AbstractValidator<GetNewsQuery>
{
    public GetNewsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Headline).MaximumLength(200);
    }
}
