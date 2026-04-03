using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
public sealed class TrendsController(
    TrendService trendService,
    IValidator<GetTrendsQuery> getTrendsValidator,
    IValidator<GetAlertTrendsRoute> getAlertTrendsRouteValidator) : ControllerBase
{
    [HttpGet("api/v1/regions/{regionId:guid}/trends")]
    public async Task<ActionResult<IReadOnlyList<TrendSeriesDto>>> GetRegionTrends(
        Guid regionId,
        [FromQuery] GetTrendsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getTrendsValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var filters = MapFilters(query);
        var series = await trendService.GetAggregateByRegionAsync(regionId, filters, cancellationToken);
        var totalCount = await trendService.CountAggregateByRegionAsync(regionId, filters, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return Ok(series);
    }

    [HttpGet("api/v1/regions/{regionId:guid}/alerts/{alertId:guid}/trends")]
    public async Task<ActionResult<TrendSeriesDto>> GetAlertTrends(
        Guid regionId,
        Guid alertId,
        [FromQuery] GetTrendsQuery query,
        CancellationToken cancellationToken)
    {
        var routeModel = new GetAlertTrendsRoute
        {
            RegionId = regionId,
            AlertId = alertId
        };

        var routeValidationResult = await getAlertTrendsRouteValidator.ValidateAsync(routeModel, cancellationToken);
        if (!routeValidationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(routeValidationResult.ToDictionary()));
        }

        var queryValidationResult = await getTrendsValidator.ValidateAsync(query, cancellationToken);
        if (!queryValidationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(queryValidationResult.ToDictionary()));
        }

        var series = await trendService.GetByAlertAsync(regionId, alertId, MapFilters(query), cancellationToken);
        if (series is null)
        {
            return NotFound();
        }

        return Ok(series);
    }

    private static TrendFilters MapFilters(GetTrendsQuery query)
    {
        return new TrendFilters
        {
            Disease = query.Disease,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
