using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/v1/regions/{regionId:guid}/alerts")]
public sealed class AlertsController(
    AlertService alertService,
    IValidator<GetAlertsQuery> getAlertsValidator,
    IValidator<GetAlertByIdRoute> getAlertByIdValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertListDto>>> GetAlerts(
        Guid regionId,
        [FromQuery] GetAlertsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getAlertsValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var filters = new AlertFilters
        {
            Severity = query.Severity,
            Disease = query.Disease,
            Status = query.Status,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            Page = query.Page,
            PageSize = query.PageSize,
            SortBy = query.SortBy,
            SortDirection = query.SortDirection
        };

        var alerts = await alertService.GetByRegionAsync(regionId, filters, cancellationToken);
        var totalCount = await alertService.CountByRegionAsync(regionId, filters, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return Ok(alerts.Select(MapListDto).ToList());
    }

    [HttpGet("{alertId:guid}")]
    public async Task<ActionResult<AlertDetailDto>> GetAlertById(Guid regionId, Guid alertId, CancellationToken cancellationToken)
    {
        var routeModel = new GetAlertByIdRoute
        {
            RegionId = regionId,
            AlertId = alertId
        };

        var validationResult = await getAlertByIdValidator.ValidateAsync(routeModel, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var alert = await alertService.GetByIdAsync(regionId, alertId, cancellationToken);
        if (alert is null)
        {
            return NotFound();
        }

        return Ok(MapDetailDto(alert));
    }

    private static AlertListDto MapListDto(HealthAlert alert)
    {
        return new AlertListDto
        {
            Id = alert.Id,
            RegionId = alert.RegionId,
            Disease = alert.Disease,
            Title = alert.Title,
            Summary = alert.Summary,
            Severity = alert.Severity,
            CaseCount = alert.CaseCount,
            SourceAttribution = alert.SourceAttribution,
            SourceDate = alert.SourceDate,
            CreatedAt = alert.CreatedAt
        };
    }

    private static AlertDetailDto MapDetailDto(HealthAlert alert)
    {
        return new AlertDetailDto
        {
            Id = alert.Id,
            RegionId = alert.RegionId,
            Disease = alert.Disease,
            Title = alert.Title,
            Summary = alert.Summary,
            Severity = alert.Severity,
            CaseCount = alert.CaseCount,
            SourceAttribution = alert.SourceAttribution,
            SourceDate = alert.SourceDate,
            Status = alert.Status,
            CreatedAt = alert.CreatedAt,
            Trends = alert.DiseaseTrends
                .OrderByDescending(trend => trend.Date)
                .Select(trend => new DiseaseTrendDto
                {
                    Date = trend.Date,
                    CaseCount = trend.CaseCount,
                    Source = trend.Source,
                    SourceDate = trend.SourceDate,
                    Notes = trend.Notes
                })
                .ToList()
        };
    }
}
