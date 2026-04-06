namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotEnvironmentalSignalSummary
{
    public Guid ViolationId { get; set; }

    public string WaterSystemName { get; set; } = string.Empty;

    public string ViolationCategory { get; set; } = string.Empty;

    public string RuleName { get; set; } = string.Empty;

    public string? ContaminantName { get; set; }

    public string Summary { get; set; } = string.Empty;

    public bool IsOpen { get; set; }

    public int? PopulationServed { get; set; }

    public DateTime? IdentifiedAt { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }
}
