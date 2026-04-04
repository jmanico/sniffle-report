using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class HealthAlert : SoftDeletableEntityBase
{
    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public string Disease { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; }

    public int CaseCount { get; set; }

    public string SourceAttribution { get; set; } = string.Empty;

    public DateTime SourceDate { get; set; }

    public AlertStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? IngestedRecordId { get; set; }

    public IngestedRecord? IngestedRecord { get; set; }

    public ICollection<DiseaseTrend> DiseaseTrends { get; set; } = [];
}
