namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotAlertSummary
{
    public Guid AlertId { get; set; }

    public string Disease { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public int CaseCount { get; set; }

    public DateTime SourceDate { get; set; }
}
