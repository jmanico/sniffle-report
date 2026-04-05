using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Controllers.Public;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Models.Snapshots;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class DashboardControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetDashboard_ReturnsSnapshotData()
    {
        await using var dbContext = CreateDbContextWithSnapshot();
        var controller = CreateController(dbContext);
        var regionId = await dbContext.Regions.Select(r => r.Id).FirstAsync();

        var result = await controller.GetDashboard(regionId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<RegionDashboardDto>(okResult.Value);
        Assert.Equal(regionId, dto.RegionId);
        Assert.Equal(2, dto.PublishedAlertCount);
        Assert.Equal(2, dto.TopAlerts.Count);
        Assert.Equal(1, dto.ResourceCounts.Clinic);
    }

    [Fact]
    public async Task GetDashboard_Returns404WhenNoSnapshot()
    {
        await using var dbContext = CreateDbContextWithoutSnapshot();
        var controller = CreateController(dbContext);
        var regionId = await dbContext.Regions.Select(r => r.Id).FirstAsync();

        var result = await controller.GetDashboard(regionId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static DashboardController CreateController(AppDbContext dbContext)
    {
        IValidator<GetDashboardRoute> validator = new GetDashboardRouteValidator();
        return new DashboardController(dbContext, validator);
    }

    private static AppDbContext CreateDbContextWithSnapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);

        var region = new Region { Name = "Travis County", Type = RegionType.County, State = "TX" };
        dbContext.Regions.Add(region);

        var topAlerts = new List<SnapshotAlertSummary>
        {
            new() { AlertId = Guid.NewGuid(), Disease = "Influenza", Title = "Alert 1", Severity = "High", CaseCount = 30, SourceDate = DateTime.UtcNow },
            new() { AlertId = Guid.NewGuid(), Disease = "RSV", Title = "Alert 2", Severity = "Moderate", CaseCount = 15, SourceDate = DateTime.UtcNow }
        };

        var resourceCounts = new SnapshotResourceCounts { Clinic = 1, Pharmacy = 2, Total = 3 };

        dbContext.RegionSnapshots.Add(new RegionSnapshot
        {
            Region = region,
            ComputedAt = DateTime.UtcNow,
            PublishedAlertCount = 2,
            TopAlertsJson = JsonSerializer.Serialize(topAlerts, JsonOptions),
            TrendHighlightsJson = "[]",
            ResourceCountsJson = JsonSerializer.Serialize(resourceCounts, JsonOptions),
            PreventionHighlightsJson = "[]",
            NewsHighlightsJson = "[]"
        });

        dbContext.SaveChanges();
        return dbContext;
    }

    private static AppDbContext CreateDbContextWithoutSnapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        dbContext.Regions.Add(new Region { Name = "Empty Region", Type = RegionType.State, State = "XX" });
        dbContext.SaveChanges();
        return dbContext;
    }
}
