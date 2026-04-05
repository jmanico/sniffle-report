namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotAlertSummary
{
    public Guid AlertId { get; set; }

    public string Disease { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public int CaseCount { get; set; }

    public string SourceAttribution { get; set; } = string.Empty;

    public DateTime SourceDate { get; set; }

    public int? PreviousCaseCount { get; set; }

    public double? WowChangePercent { get; set; }

    public DateTime? PreviousSourceDate { get; set; }
}
