using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class AlertServiceTests
{
    [Fact]
    public async Task GetByRegionAsync_FiltersBySeverityAndPublishedStatus()
    {
        await using var dbContext = CreateDbContext();
        var service = new AlertService(dbContext, new RegionHierarchyService(dbContext));
        var county = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");

        var results = await service.GetByRegionAsync(
            county.Id,
            new AlertFilters
            {
                Severity = AlertSeverity.High,
                Page = 1,
                PageSize = 25
            });

        Assert.Single(results);
        Assert.Equal("High priority county alert", results[0].Title);
    }

    [Fact]
    public async Task GetByRegionAsync_IncludesChildRegionAlertsForParentRegion()
    {
        await using var dbContext = CreateDbContext();
        var service = new AlertService(dbContext, new RegionHierarchyService(dbContext));
        var county = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");

        var results = await service.GetByRegionAsync(
            county.Id,
            new AlertFilters
            {
                Page = 1,
                PageSize = 25
            });

        Assert.Equal(2, results.Count);
        Assert.Contains(results, alert => alert.Title == "Zip code alert");
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnAlertFromDifferentRegion()
    {
        await using var dbContext = CreateDbContext();
        var service = new AlertService(dbContext, new RegionHierarchyService(dbContext));
        var travis = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");
        var otherAlert = await dbContext.HealthAlerts.SingleAsync(alert => alert.Title == "Other county alert");

        var result = await service.GetByIdAsync(travis.Id, otherAlert.Id);

        Assert.Null(result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);

        var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
        var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };
        var zip = new Region { Name = "78701", Type = RegionType.Zip, State = "TX", Parent = travis };
        var cook = new Region { Name = "Cook County", Type = RegionType.County, State = "IL" };

        dbContext.Regions.AddRange(texas, travis, zip, cook);

        dbContext.HealthAlerts.AddRange(
            new HealthAlert
            {
                Region = travis,
                Title = "High priority county alert",
                Disease = "Influenza",
                Summary = "County scoped sample alert",
                Severity = AlertSeverity.High,
                CaseCount = 30,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published,
                CreatedAt = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc)
            },
            new HealthAlert
            {
                Region = zip,
                Title = "Zip code alert",
                Disease = "Influenza",
                Summary = "Child region alert",
                Severity = AlertSeverity.Moderate,
                CaseCount = 10,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published,
                CreatedAt = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc)
            },
            new HealthAlert
            {
                Region = travis,
                Title = "Draft county alert",
                Disease = "Influenza",
                Summary = "Should never leak publicly",
                Severity = AlertSeverity.Critical,
                CaseCount = 99,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Draft,
                CreatedAt = new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc)
            },
            new HealthAlert
            {
                Region = cook,
                Title = "Other county alert",
                Disease = "Influenza",
                Summary = "Different region",
                Severity = AlertSeverity.High,
                CaseCount = 12,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 1, 14, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published,
                CreatedAt = new DateTime(2026, 1, 14, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 14, 0, 0, 0, DateTimeKind.Utc)
            });

        dbContext.SaveChanges();

        return dbContext;
    }
}
