namespace SniffleReport.Api.Models.Entities;

public sealed class DiseaseTrend : EntityBase
{
    public Guid AlertId { get; set; }

    public HealthAlert Alert { get; set; } = null!;

    public DateTime Date { get; set; }

    public int CaseCount { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTime SourceDate { get; set; }

    public string? Notes { get; set; }

    public Guid? IngestedRecordId { get; set; }

    public IngestedRecord? IngestedRecord { get; set; }
}
