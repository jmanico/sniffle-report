namespace SniffleReport.Api.Models.Entities;

public sealed class ShortageAreaDesignation : EntityBase
{
    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public string ExternalSourceId { get; set; } = string.Empty;

    public string AreaName { get; set; } = string.Empty;

    public string Discipline { get; set; } = string.Empty;

    public string DesignationType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? PopulationGroup { get; set; }

    public int? HpsaScore { get; set; }

    public decimal? PopulationToProviderRatio { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }
}
