using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion;

public sealed class AlertThresholdService(
    AppDbContext dbContext,
    IOptions<FeedIngestionOptions> options,
    ILogger<AlertThresholdService> logger)
{
    private readonly FeedIngestionOptions _options = options.Value;

    public async Task<IReadOnlyList<HealthAlert>> EvaluateAndPromoteAsync(
        Guid regionId,
        string disease,
        string feedSourceName,
        CancellationToken ct)
    {
        var thresholds = _options.Thresholds;
        var cutoffDate = DateTime.UtcNow.AddDays(-7 * thresholds.WeeksToEvaluate);

        // Get recent trend data points for this region + disease
        var recentTrends = await dbContext.DiseaseTrends
            .Where(t => t.Alert.RegionId == regionId
                && t.Alert.Disease == disease
                && t.Date >= cutoffDate)
            .OrderByDescending(t => t.Date)
            .Select(t => new { t.Date, t.CaseCount })
            .ToListAsync(ct);

        if (recentTrends.Count < 2)
            return [];

        // Group by week and compute WoW change
        var weeklyData = recentTrends
            .GroupBy(t => GetIsoWeek(t.Date))
            .OrderByDescending(g => g.Key)
            .Select(g => new { Week = g.Key, MaxCount = g.Max(x => x.CaseCount) })
            .Take(thresholds.WeeksToEvaluate)
            .ToList();

        if (weeklyData.Count < 2)
            return [];

        var currentWeek = weeklyData[0];
        var previousWeek = weeklyData[1];

        if (previousWeek.MaxCount <= 0)
            return [];

        var wowPercentage = ((double)(currentWeek.MaxCount - previousWeek.MaxCount) / previousWeek.MaxCount) * 100;
        var absoluteCount = currentWeek.MaxCount;

        var severity = EvaluateSeverity(wowPercentage, absoluteCount, thresholds);
        if (severity is null)
            return [];

        // Check for existing non-archived alert at this severity or higher
        var existingAlert = await dbContext.HealthAlerts
            .FirstOrDefaultAsync(
                a => a.RegionId == regionId
                    && a.Disease == disease
                    && a.Status != AlertStatus.Archived
                    && a.Severity >= severity.Value,
                ct);

        if (existingAlert is not null)
        {
            logger.LogDebug(
                "Existing alert {AlertId} already covers {Disease} in region {RegionId} at severity {Severity}",
                existingAlert.Id, disease, regionId, existingAlert.Severity);
            return [];
        }

        // Create a Draft alert for admin review
        var alert = new HealthAlert
        {
            RegionId = regionId,
            Disease = disease,
            Title = $"{disease} — {severity.Value} severity threshold crossed",
            Summary = $"Week-over-week change: {wowPercentage:F1}%. Current count: {absoluteCount}. " +
                      $"Previous week: {previousWeek.MaxCount}. Auto-generated from {feedSourceName}.",
            Severity = severity.Value,
            CaseCount = absoluteCount,
            SourceAttribution = $"{feedSourceName} (auto-generated)",
            SourceDate = DateTime.UtcNow,
            Status = AlertStatus.Draft
        };
        dbContext.HealthAlerts.Add(alert);

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            _options.SystemUserId,
            AuditLogAction.FeedIngest,
            nameof(HealthAlert),
            alert.Id,
            null,
            new
            {
                alert.RegionId,
                alert.Disease,
                alert.Severity,
                alert.CaseCount,
                WowPercentage = wowPercentage,
                PreviousWeekCount = previousWeek.MaxCount
            },
            $"Threshold-promoted alert from {feedSourceName}"));

        logger.LogInformation(
            "Promoted {Disease} to {Severity} Draft alert for region {RegionId} " +
            "(WoW: {WowPct:F1}%, count: {Count})",
            disease, severity.Value, regionId, wowPercentage, absoluteCount);

        return [alert];
    }

    private static AlertSeverity? EvaluateSeverity(
        double wowPercentage,
        int absoluteCount,
        AlertThresholdOptions thresholds)
    {
        if (wowPercentage >= thresholds.CriticalWowPercentage
            && absoluteCount >= thresholds.CriticalMinAbsoluteCount)
            return AlertSeverity.Critical;

        if (wowPercentage >= thresholds.HighWowPercentage
            || absoluteCount >= thresholds.HighMinAbsoluteCount)
            return AlertSeverity.High;

        if (wowPercentage >= thresholds.ModerateWowPercentage
            && absoluteCount >= thresholds.ModerateMinAbsoluteCount)
            return AlertSeverity.Moderate;

        return null;
    }

    private static int GetIsoWeek(DateTime date)
    {
        return System.Globalization.ISOWeek.GetWeekOfYear(date) + date.Year * 100;
    }
}
