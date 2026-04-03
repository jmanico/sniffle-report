using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAlertTrendsRouteValidator : AbstractValidator<GetAlertTrendsRoute>
{
    public GetAlertTrendsRouteValidator()
    {
        RuleFor(x => x.RegionId).NotEmpty();
        RuleFor(x => x.AlertId).NotEmpty();
    }
}
