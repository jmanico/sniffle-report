using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class FactCheckHistory : EntityBase
{
    public Guid FactCheckId { get; set; }

    public FactCheck FactCheck { get; set; } = null!;

    public FactCheckStatus? PreviousStatus { get; set; }

    public FactCheckStatus NewStatus { get; set; }

    public Guid ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string Justification { get; set; } = string.Empty;

    public string SourcesJson { get; set; } = "[]";
}
