namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotNewsSummary
{
    public Guid NewsItemId { get; set; }

    public string Headline { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public string? FactCheckStatus { get; set; }
}
