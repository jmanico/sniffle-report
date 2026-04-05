using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Models.Snapshots;
using SniffleReport.Api.Services;
using SniffleReport.Api.Services.Snapshots;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class RegionSnapshotBuilderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task RebuildAllAsync_CreatesSnapshotsForAllRegions()
    {
        await using var dbContext = CreateDbContext();
        var builder = CreateBuilder(dbContext);

        await builder.RebuildAllAsync(CancellationToken.None);

        var snapshots = await dbContext.RegionSnapshots.ToListAsync();
        var regionCount = await dbContext.Regions.CountAsync();
        Assert.Equal(regionCount, snapshots.Count);
    }

    [Fact]
    public async Task RebuildAllAsync_RollsUpChildRegionAlertsIntoParent()
    {
        await using var dbContext = CreateDbContext();
        var builder = CreateBuilder(dbContext);

        await builder.RebuildAllAsync(CancellationToken.None);

        var texas = await dbContext.Regions.SingleAsync(r => r.Name == "Texas");
        var snapshot = await dbContext.RegionSnapshots.SingleAsync(s => s.RegionId == texas.Id);

        // Texas should include alerts from Travis County and its zip code child
        Assert.Equal(2, snapshot.PublishedAlertCount);
        var topAlerts = JsonSerializer.Deserialize<List<SnapshotAlertSummary>>(snapshot.TopAlertsJson, JsonOptions)!;
        Assert.Equal(2, topAlerts.Count);
    }

    [Fact]
    public async Task RebuildAllAsync_ExcludesDraftAndArchivedAlerts()
    {
        await using var dbContext = CreateDbContext();
        var builder = CreateBuilder(dbContext);

        await builder.RebuildAllAsync(CancellationToken.None);

        var travis = await dbContext.Regions.SingleAsync(r => r.Name == "Travis County");
        var snapshot = await dbContext.RegionSnapshots.SingleAsync(s => s.RegionId == travis.Id);

        // Travis has 1 Published + 1 Draft alert; only Published should be counted
        // Plus the child zip code has 1 Published alert = 2 total
        Assert.Equal(2, snapshot.PublishedAlertCount);
    }

    [Fact]
    public async Task RebuildAllAsync_EmptyRegionProducesValidSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var builder = CreateBuilder(dbContext);

        await builder.RebuildAllAsync(CancellationToken.None);

        var cook = await dbContext.Regions.SingleAsync(r => r.Name == "Cook County");
        var snapshot = await dbContext.RegionSnapshots.SingleAsync(s => s.RegionId == cook.Id);

        Assert.Equal(0, snapshot.PublishedAlertCount);
        var topAlerts = JsonSerializer.Deserialize<List<SnapshotAlertSummary>>(snapshot.TopAlertsJson, JsonOptions)!;
        Assert.Empty(topAlerts);
        var resourceCounts = JsonSerializer.Deserialize<SnapshotResourceCounts>(snapshot.ResourceCountsJson, JsonOptions)!;
        Assert.Equal(0, resourceCounts.Total);
    }

    [Fact]
    public async Task RebuildAllAsync_UpdatesExistingSnapshots()
    {
        await using var dbContext = CreateDbContext();
        var builder = CreateBuilder(dbContext);

        // First build
        await builder.RebuildAllAsync(CancellationToken.None);
        var firstCount = await dbContext.RegionSnapshots.CountAsync();

        // Add another published alert
        var travis = await dbContext.Regions.SingleAsync(r => r.Name == "Travis County");
        dbContext.HealthAlerts.Add(new HealthAlert
        {
            Region = travis,
            Title = "New alert",
            Disease = "RSV",
            Summary = "Added after first snapshot",
            Severity = AlertSeverity.Critical,
            CaseCount = 100,
            SourceAttribution = "Test",
            SourceDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Second build (upsert)
        await builder.RebuildAllAsync(CancellationToken.None);

        var secondCount = await dbContext.RegionSnapshots.CountAsync();
        Assert.Equal(firstCount, secondCount); // Same number of rows

        var snapshot = await dbContext.RegionSnapshots.SingleAsync(s => s.RegionId == travis.Id);
        Assert.Equal(3, snapshot.PublishedAlertCount); // 2 original + 1 new
    }

    [Fact]
    public async Task RebuildAllAsync_CountsResourcesByType()
    {
        await using var dbContext = CreateDbContext();
        var builder = CreateBuilder(dbContext);

        await builder.RebuildAllAsync(CancellationToken.None);

        var travis = await dbContext.Regions.SingleAsync(r => r.Name == "Travis County");
        var snapshot = await dbContext.RegionSnapshots.SingleAsync(s => s.RegionId == travis.Id);

        var counts = JsonSerializer.Deserialize<SnapshotResourceCounts>(snapshot.ResourceCountsJson, JsonOptions)!;
        Assert.Equal(1, counts.Clinic);
        Assert.Equal(1, counts.Pharmacy);
        Assert.Equal(0, counts.Hospital);
        Assert.Equal(2, counts.Total);
    }

    [Fact]
    public async Task RebuildAllAsync_ComputesTrendHighlights()
    {
        await using var dbContext = CreateDbContext();
        var builder = CreateBuilder(dbContext);

        await builder.RebuildAllAsync(CancellationToken.None);

        var travis = await dbContext.Regions.SingleAsync(r => r.Name == "Travis County");
        var snapshot = await dbContext.RegionSnapshots.SingleAsync(s => s.RegionId == travis.Id);

        var trends = JsonSerializer.Deserialize<List<SnapshotTrendHighlight>>(snapshot.TrendHighlightsJson, JsonOptions)!;
        Assert.Single(trends);
        Assert.Equal("Influenza", trends[0].Disease);
        Assert.Equal(30, trends[0].LatestCaseCount);
        Assert.Equal(10, trends[0].PreviousCaseCount);
        Assert.True(trends[0].WowChangePercent > 0);
    }

    private static RegionSnapshotBuilder CreateBuilder(AppDbContext dbContext)
    {
        var hierarchyService = new RegionHierarchyService(dbContext);
        var options = Options.Create(new SnapshotOptions());
        var logger = NullLogger<RegionSnapshotBuilder>.Instance;
        return new RegionSnapshotBuilder(dbContext, hierarchyService, options, logger);
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

        var travisAlert = new HealthAlert
        {
            Region = travis,
            Title = "County alert",
            Disease = "Influenza",
            Summary = "Published alert",
            Severity = AlertSeverity.High,
            CaseCount = 30,
            SourceAttribution = "CDC",
            SourceDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Published,
            CreatedAt = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc)
        };

        var zipAlert = new HealthAlert
        {
            Region = zip,
            Title = "Zip alert",
            Disease = "Influenza",
            Summary = "Child region alert",
            Severity = AlertSeverity.Moderate,
            CaseCount = 10,
            SourceAttribution = "CDC",
            SourceDate = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Published,
            CreatedAt = new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc)
        };

        var draftAlert = new HealthAlert
        {
            Region = travis,
            Title = "Draft alert",
            Disease = "RSV",
            Summary = "Not published",
            Severity = AlertSeverity.Critical,
            CaseCount = 99,
            SourceAttribution = "CDC",
            SourceDate = new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Draft,
            CreatedAt = new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc)
        };

        dbContext.HealthAlerts.AddRange(travisAlert, zipAlert, draftAlert);

        // Trends for the county alert (used for WoW calculation)
        dbContext.DiseaseTrends.AddRange(
            new DiseaseTrend
            {
                Alert = travisAlert,
                Date = new DateTime(2025, 12, 27, 0, 0, 0, DateTimeKind.Utc),
                CaseCount = 10,
                Source = "CDC",
                SourceDate = new DateTime(2025, 12, 28, 0, 0, 0, DateTimeKind.Utc)
            },
            new DiseaseTrend
            {
                Alert = travisAlert,
                Date = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                CaseCount = 30,
                Source = "CDC",
                SourceDate = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc)
            });

        // Resources in Travis County
        dbContext.LocalResources.AddRange(
            new LocalResource
            {
                Region = travis,
                Name = "Test Clinic",
                Type = ResourceType.Clinic,
                Address = "123 Main St"
            },
            new LocalResource
            {
                Region = travis,
                Name = "Test Pharmacy",
                Type = ResourceType.Pharmacy,
                Address = "456 Oak Ave"
            });

        // Prevention guide
        dbContext.PreventionGuides.Add(new PreventionGuide
        {
            Region = travis,
            Disease = "Influenza",
            Title = "Flu Prevention Guide",
            Content = "Get vaccinated"
        });

        // News item
        dbContext.NewsItems.Add(new NewsItem
        {
            Region = travis,
            Headline = "Flu Cases Rising",
            Content = "Cases up 200%",
            SourceUrl = "https://example.com/news",
            PublishedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        return dbContext;
    }
}
