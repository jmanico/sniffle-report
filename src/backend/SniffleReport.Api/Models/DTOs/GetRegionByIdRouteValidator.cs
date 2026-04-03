using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetRegionByIdRouteValidator : AbstractValidator<GetRegionByIdRoute>
{
    public GetRegionByIdRouteValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
