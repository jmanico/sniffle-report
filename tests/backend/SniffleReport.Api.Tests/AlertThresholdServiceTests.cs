using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class AlertThresholdServiceTests
{
    [Fact]
    public async Task EvaluateAndPromoteAsync_CreatesModerateAlertOnThresholdCrossing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var texas = await db.Regions.SingleAsync(r => r.Name == "Texas");

        // Seed trends: week1 = 10, week2 = 16 (60% increase, above Moderate threshold)
        // Existing alert at Moderate severity should prevent promotion
        var alert = new HealthAlert
        {
            Region = texas,
            Disease = "Flu",
            Title = "Flu tracker",
            Summary = "Test",
            Severity = AlertSeverity.Moderate,
            CaseCount = 16,
            SourceAttribution = "Test",
            SourceDate = DateTime.UtcNow,
            Status = AlertStatus.Draft
        };
        db.HealthAlerts.Add(alert);

        db.DiseaseTrends.AddRange(
            new DiseaseTrend
            {
                Alert = alert,
                Date = DateTime.UtcNow.AddDays(-14),
                CaseCount = 10,
                Source = "Test",
                SourceDate = DateTime.UtcNow.AddDays(-14)
            },
            new DiseaseTrend
            {
                Alert = alert,
                Date = DateTime.UtcNow.AddDays(-7),
                CaseCount = 16,
                Source = "Test",
                SourceDate = DateTime.UtcNow.AddDays(-7)
            });
        await db.SaveChangesAsync();

        var promoted = await service.EvaluateAndPromoteAsync(texas.Id, "Flu", "TestFeed", CancellationToken.None);

        Assert.Empty(promoted); // Existing alert already covers this
    }

    [Fact]
    public async Task EvaluateAndPromoteAsync_CreatesHighAlertWhenNoExistingAlert()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var texas = await db.Regions.SingleAsync(r => r.Name == "Texas");

        // Create an alert + trends with high WoW increase, but archive the alert
        var archivedAlert = new HealthAlert
        {
            Region = texas,
            Disease = "Measles",
            Title = "Old measles alert",
            Summary = "Archived",
            Severity = AlertSeverity.Low,
            CaseCount = 55,
            SourceAttribution = "Test",
            SourceDate = DateTime.UtcNow,
            Status = AlertStatus.Archived
        };
        db.HealthAlerts.Add(archivedAlert);

        db.DiseaseTrends.AddRange(
            new DiseaseTrend
            {
                Alert = archivedAlert,
                Date = DateTime.UtcNow.AddDays(-14),
                CaseCount = 20,
                Source = "Test",
                SourceDate = DateTime.UtcNow.AddDays(-14)
            },
            new DiseaseTrend
            {
                Alert = archivedAlert,
                Date = DateTime.UtcNow.AddDays(-7),
                CaseCount = 55,
                Source = "Test",
                SourceDate = DateTime.UtcNow.AddDays(-7)
            });
        await db.SaveChangesAsync();

        var promoted = await service.EvaluateAndPromoteAsync(texas.Id, "Measles", "TestFeed", CancellationToken.None);

        Assert.Single(promoted);
        Assert.Equal(AlertSeverity.High, promoted[0].Severity);
        Assert.Equal(AlertStatus.Draft, promoted[0].Status);
        Assert.Contains("auto-generated", promoted[0].SourceAttribution);
    }

    [Fact]
    public async Task EvaluateAndPromoteAsync_ReturnsEmptyWhenInsufficientData()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var texas = await db.Regions.SingleAsync(r => r.Name == "Texas");

        var promoted = await service.EvaluateAndPromoteAsync(texas.Id, "Unknown", "TestFeed", CancellationToken.None);

        Assert.Empty(promoted);
    }

    [Fact]
    public async Task EvaluateAndPromoteAsync_NeverAutoPublishes()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var texas = await db.Regions.SingleAsync(r => r.Name == "Texas");

        // Create massive spike to trigger critical threshold
        var alert = new HealthAlert
        {
            Region = texas,
            Disease = "TestDisease",
            Title = "Archived alert",
            Summary = "Test",
            Severity = AlertSeverity.Low,
            CaseCount = 500,
            SourceAttribution = "Test",
            SourceDate = DateTime.UtcNow,
            Status = AlertStatus.Archived
        };
        db.HealthAlerts.Add(alert);

        db.DiseaseTrends.AddRange(
            new DiseaseTrend
            {
                Alert = alert,
                Date = DateTime.UtcNow.AddDays(-14),
                CaseCount = 100,
                Source = "Test",
                SourceDate = DateTime.UtcNow.AddDays(-14)
            },
            new DiseaseTrend
            {
                Alert = alert,
                Date = DateTime.UtcNow.AddDays(-7),
                CaseCount = 500,
                Source = "Test",
                SourceDate = DateTime.UtcNow.AddDays(-7)
            });
        await db.SaveChangesAsync();

        var promoted = await service.EvaluateAndPromoteAsync(texas.Id, "TestDisease", "TestFeed", CancellationToken.None);

        Assert.Single(promoted);
        Assert.Equal(AlertStatus.Draft, promoted[0].Status);
    }

    private static AlertThresholdService CreateService(AppDbContext db)
    {
        var options = Options.Create(new FeedIngestionOptions());
        return new AlertThresholdService(db, options, NullLogger<AlertThresholdService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.Regions.Add(new Region { Name = "Texas", Type = RegionType.State, State = "TX" });
        db.SaveChanges();

        return db;
    }
}
