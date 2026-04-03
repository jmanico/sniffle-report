using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAlertByIdRouteValidator : AbstractValidator<GetAlertByIdRoute>
{
    public GetAlertByIdRouteValidator()
    {
        RuleFor(x => x.RegionId).NotEmpty();
        RuleFor(x => x.AlertId).NotEmpty();
    }
}
