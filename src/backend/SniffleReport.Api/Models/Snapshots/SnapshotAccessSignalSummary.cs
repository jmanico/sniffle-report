namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotAccessSignalSummary
{
    public Guid DesignationId { get; set; }

    public string AreaName { get; set; } = string.Empty;

    public string Discipline { get; set; } = string.Empty;

    public string DesignationType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? PopulationGroup { get; set; }

    public int? HpsaScore { get; set; }

    public decimal? PopulationToProviderRatio { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }
}
