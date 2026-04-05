using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetDashboardRouteValidator : AbstractValidator<GetDashboardRoute>
{
    public GetDashboardRouteValidator()
    {
        RuleFor(x => x.RegionId)
            .NotEmpty();
    }
}
