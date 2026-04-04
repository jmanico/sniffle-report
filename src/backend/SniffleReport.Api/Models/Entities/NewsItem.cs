namespace SniffleReport.Api.Models.Entities;

public sealed class NewsItem : SoftDeletableEntityBase
{
    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public string Headline { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? IngestedRecordId { get; set; }

    public IngestedRecord? IngestedRecord { get; set; }

    public FactCheck? FactCheck { get; set; }
}
