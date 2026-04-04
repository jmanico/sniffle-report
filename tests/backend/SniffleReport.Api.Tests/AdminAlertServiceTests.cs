using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class AdminAlertServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsAlert()
    {
        await using var dbContext = CreateDbContext();
        var service = new AlertService(dbContext);
        var regionId = await dbContext.Regions.Select(region => region.Id).FirstAsync();

        var created = await service.CreateAsync(new CreateAlertRequest
        {
            RegionId = regionId,
            Disease = "Influenza A",
            Title = "Created alert",
            Summary = "Created via admin service",
            Severity = AlertSeverity.High,
            CaseCount = 12,
            SourceAttribution = "Sample",
            SourceDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Draft
        });

        Assert.Equal("Created alert", created.Title);
        Assert.Equal(3, await dbContext.HealthAlerts.IgnoreQueryFilters().CountAsync());
        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditLogAction.Create, auditEntry.Action);
        Assert.Equal(created.Id, auditEntry.EntityId);
        Assert.Null(auditEntry.BeforeJson);
        Assert.NotNull(auditEntry.AfterJson);
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesAlertStatus()
    {
        await using var dbContext = CreateDbContext();
        var service = new AlertService(dbContext);
        var alertId = await dbContext.HealthAlerts.Select(alert => alert.Id).FirstAsync();

        var updated = await service.UpdateStatusAsync(alertId, new UpdateAlertStatusRequest
        {
            Status = AlertStatus.Archived,
            Justification = "Archiving stale alert"
        });

        Assert.NotNull(updated);
        Assert.Equal(AlertStatus.Archived, updated!.Status);
        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditLogAction.StatusChange, auditEntry.Action);
        Assert.Equal("Archiving stale alert", auditEntry.Justification);
        Assert.NotNull(auditEntry.BeforeJson);
        Assert.NotNull(auditEntry.AfterJson);
    }

    [Fact]
    public async Task SoftDeleteAsync_SetsSoftDeleteFields()
    {
        await using var dbContext = CreateDbContext();
        var service = new AlertService(dbContext);
        var alertId = await dbContext.HealthAlerts.Select(alert => alert.Id).FirstAsync();

        var deleted = await service.SoftDeleteAsync(alertId, "Remove old content");

        Assert.True(deleted);

        var alert = await dbContext.HealthAlerts.IgnoreQueryFilters().SingleAsync(item => item.Id == alertId);
        Assert.True(alert.IsDeleted);
        Assert.Null(alert.DeletedBy);
        Assert.NotNull(alert.DeletedAt);

        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditLogAction.Delete, auditEntry.Action);
        Assert.Equal("Remove old content", auditEntry.Justification);
        Assert.NotNull(auditEntry.BeforeJson);
        Assert.NotNull(auditEntry.AfterJson);
    }

    [Fact]
    public async Task UpdateAsync_WritesAuditSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var service = new AlertService(dbContext);
        var alertId = await dbContext.HealthAlerts.Select(alert => alert.Id).FirstAsync();
        var regionId = await dbContext.Regions.Select(region => region.Id).FirstAsync();

        var updated = await service.UpdateAsync(alertId, new UpdateAlertRequest
        {
            RegionId = regionId,
            Disease = "Updated disease",
            Title = "Updated title",
            Summary = "Updated summary",
            Severity = AlertSeverity.Moderate,
            CaseCount = 22,
            SourceAttribution = "Updated source",
            SourceDate = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated!.Title);

        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditLogAction.Update, auditEntry.Action);
        Assert.NotNull(auditEntry.BeforeJson);
        Assert.NotNull(auditEntry.AfterJson);
        Assert.Contains("Updated title", auditEntry.AfterJson);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        var region = new Region { Name = "Travis County", Type = RegionType.County, State = "TX" };

        dbContext.Regions.Add(region);
        dbContext.HealthAlerts.AddRange(
            new HealthAlert
            {
                Region = region,
                Title = "Draft alert",
                Disease = "Influenza",
                Summary = "Draft",
                Severity = AlertSeverity.Low,
                CaseCount = 4,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Draft
            },
            new HealthAlert
            {
                Region = region,
                Title = "Published alert",
                Disease = "RSV",
                Summary = "Published",
                Severity = AlertSeverity.High,
                CaseCount = 10,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published
            });

        dbContext.SaveChanges();

        return dbContext;
    }
}
