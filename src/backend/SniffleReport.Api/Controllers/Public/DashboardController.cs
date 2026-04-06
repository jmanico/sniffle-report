using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Snapshots;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/v1/regions/{regionId:guid}/dashboard")]
public sealed class DashboardController(
    AppDbContext dbContext,
    IValidator<GetDashboardRoute> validator) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<ActionResult<RegionDashboardDto>> GetDashboard(
        Guid regionId,
        CancellationToken cancellationToken)
    {
        var routeModel = new GetDashboardRoute { RegionId = regionId };
        var validationResult = await validator.ValidateAsync(routeModel, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var snapshot = await dbContext.RegionSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.RegionId == regionId, cancellationToken);

        if (snapshot is null)
        {
            return NotFound();
        }

        var dto = new RegionDashboardDto
        {
            RegionId = snapshot.RegionId,
            ComputedAt = snapshot.ComputedAt,
            PublishedAlertCount = snapshot.PublishedAlertCount,
            TopAlerts = JsonSerializer.Deserialize<List<SnapshotAlertSummary>>(snapshot.TopAlertsJson, JsonOptions) ?? [],
            TrendHighlights = JsonSerializer.Deserialize<List<SnapshotTrendHighlight>>(snapshot.TrendHighlightsJson, JsonOptions) ?? [],
            ResourceCounts = JsonSerializer.Deserialize<SnapshotResourceCounts>(snapshot.ResourceCountsJson, JsonOptions) ?? new(),
            AccessSignals = JsonSerializer.Deserialize<List<SnapshotAccessSignalSummary>>(snapshot.AccessSignalsJson, JsonOptions) ?? [],
            EnvironmentalSignals = JsonSerializer.Deserialize<List<SnapshotEnvironmentalSignalSummary>>(snapshot.EnvironmentalSignalsJson, JsonOptions) ?? [],
            PreventionHighlights = JsonSerializer.Deserialize<List<SnapshotPreventionSummary>>(snapshot.PreventionHighlightsJson, JsonOptions) ?? [],
            NewsHighlights = JsonSerializer.Deserialize<List<SnapshotNewsSummary>>(snapshot.NewsHighlightsJson, JsonOptions) ?? []
        };

        return Ok(dto);
    }
}
