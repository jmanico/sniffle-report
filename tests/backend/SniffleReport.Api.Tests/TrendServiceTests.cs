using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class TrendServiceTests
{
    [Fact]
    public async Task GetByAlertAsync_DoesNotReturnTrendSeriesFromDifferentRegion()
    {
        await using var dbContext = CreateDbContext();
        var service = new TrendService(dbContext);
        var travis = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");
        var chicagoAlert = await dbContext.HealthAlerts.SingleAsync(alert => alert.Title == "Chicago flu alert");

        var result = await service.GetByAlertAsync(travis.Id, chicagoAlert.Id, new TrendFilters());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAggregateByRegionAsync_FiltersByDateRangeAndDisease()
    {
        await using var dbContext = CreateDbContext();
        var service = new TrendService(dbContext);
        var travis = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");

        var results = await service.GetAggregateByRegionAsync(
            travis.Id,
            new TrendFilters
            {
                Disease = "flu",
                DateFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTo = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Page = 1,
                PageSize = 25
            });

        Assert.Equal(2, results.Count);
        Assert.All(
            results.SelectMany(series => series.DataPoints),
            point => Assert.InRange(point.Date, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
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
        var chicago = new Region { Name = "Chicago Metro", Type = RegionType.Metro, State = "IL" };

        var travisAlert = new HealthAlert
        {
            Region = travis,
            Title = "Travis flu alert",
            Disease = "Flu",
            Summary = "County alert",
            Severity = AlertSeverity.Moderate,
            CaseCount = 30,
            SourceAttribution = "Sample source",
            SourceDate = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Published,
            DiseaseTrends =
            [
                new DiseaseTrend { Date = new DateTime(2025, 12, 20, 0, 0, 0, DateTimeKind.Utc), CaseCount = 10, Source = "County source", SourceDate = new DateTime(2025, 12, 20, 0, 0, 0, DateTimeKind.Utc) },
                new DiseaseTrend { Date = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), CaseCount = 18, Source = "County source", SourceDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc) }
            ]
        };

        var zipAlert = new HealthAlert
        {
            Region = zip,
            Title = "Downtown flu alert",
            Disease = "Flu",
            Summary = "Zip alert",
            Severity = AlertSeverity.Low,
            CaseCount = 14,
            SourceAttribution = "Sample source",
            SourceDate = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Published,
            DiseaseTrends =
            [
                new DiseaseTrend { Date = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), CaseCount = 6, Source = "Zip source", SourceDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc) },
                new DiseaseTrend { Date = new DateTime(2026, 1, 28, 0, 0, 0, DateTimeKind.Utc), CaseCount = 14, Source = "Zip source", SourceDate = new DateTime(2026, 1, 28, 0, 0, 0, DateTimeKind.Utc) }
            ]
        };

        var chicagoAlert = new HealthAlert
        {
            Region = chicago,
            Title = "Chicago flu alert",
            Disease = "Flu",
            Summary = "Metro alert",
            Severity = AlertSeverity.High,
            CaseCount = 44,
            SourceAttribution = "Sample source",
            SourceDate = new DateTime(2026, 1, 22, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Published,
            DiseaseTrends =
            [
                new DiseaseTrend { Date = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc), CaseCount = 22, Source = "Metro source", SourceDate = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc) }
            ]
        };

        dbContext.Regions.AddRange(texas, travis, zip, chicago);
        dbContext.HealthAlerts.AddRange(travisAlert, zipAlert, chicagoAlert);
        dbContext.SaveChanges();

        return dbContext;
    }
}
