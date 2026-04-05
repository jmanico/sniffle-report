namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotPreventionSummary
{
    public Guid GuideId { get; set; }

    public string Disease { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public bool HasCostTiers { get; set; }
}
