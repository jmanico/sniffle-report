using SniffleReport.Api.Models.Snapshots;

namespace SniffleReport.Api.Models.DTOs;

public sealed class RegionDashboardDto
{
    public Guid RegionId { get; set; }

    public DateTime ComputedAt { get; set; }

    public int PublishedAlertCount { get; set; }

    public IReadOnlyList<SnapshotAlertSummary> TopAlerts { get; set; } = [];

    public IReadOnlyList<SnapshotTrendHighlight> TrendHighlights { get; set; } = [];

    public SnapshotResourceCounts ResourceCounts { get; set; } = new();

    public IReadOnlyList<SnapshotPreventionSummary> PreventionHighlights { get; set; } = [];

    public IReadOnlyList<SnapshotNewsSummary> NewsHighlights { get; set; } = [];
}
