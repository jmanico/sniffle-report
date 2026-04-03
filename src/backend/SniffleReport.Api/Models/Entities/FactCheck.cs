using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class FactCheck : SoftDeletableEntityBase
{
    public Guid NewsItemId { get; set; }

    public NewsItem NewsItem { get; set; } = null!;

    public FactCheckStatus Status { get; set; }

    public string? Verdict { get; set; }

    public string SourcesJson { get; set; } = "[]";

    public DateTime? CheckedAt { get; set; }

    public Guid? CheckedBy { get; set; }

    public ICollection<FactCheckHistory> HistoryEntries { get; set; } = [];
}
