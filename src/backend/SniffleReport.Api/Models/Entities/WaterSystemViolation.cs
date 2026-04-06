namespace SniffleReport.Api.Models.Entities;

public sealed class WaterSystemViolation : EntityBase
{
    public Guid WaterSystemId { get; set; }

    public WaterSystem WaterSystem { get; set; } = null!;

    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public string ExternalSourceId { get; set; } = string.Empty;

    public string ViolationCategory { get; set; } = string.Empty;

    public string RuleName { get; set; } = string.Empty;

    public string? ContaminantName { get; set; }

    public string Summary { get; set; } = string.Empty;

    public bool IsOpen { get; set; }

    public DateTime? IdentifiedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }
}
