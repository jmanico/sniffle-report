using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetResourceByIdRouteValidator : AbstractValidator<GetResourceByIdRoute>
{
    public GetResourceByIdRouteValidator()
    {
        RuleFor(x => x.RegionId).NotEmpty();
        RuleFor(x => x.ResourceId).NotEmpty();
    }
}
