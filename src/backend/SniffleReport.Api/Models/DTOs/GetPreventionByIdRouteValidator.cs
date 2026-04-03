using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetPreventionByIdRouteValidator : AbstractValidator<GetPreventionByIdRoute>
{
    public GetPreventionByIdRouteValidator()
    {
        RuleFor(x => x.RegionId).NotEmpty();
        RuleFor(x => x.GuideId).NotEmpty();
    }
}
