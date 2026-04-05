namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotTrendHighlight
{
    public Guid AlertId { get; set; }

    public string Disease { get; set; } = string.Empty;

    public int LatestCaseCount { get; set; }

    public int PreviousCaseCount { get; set; }

    public double WowChangePercent { get; set; }

    public DateTime LatestDate { get; set; }
}
