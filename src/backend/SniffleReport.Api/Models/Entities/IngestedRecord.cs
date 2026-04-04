namespace SniffleReport.Api.Models.Entities;

public sealed class IngestedRecord : EntityBase
{
    public Guid FeedSourceId { get; set; }

    public FeedSource FeedSource { get; set; } = null!;

    public string ExternalSourceId { get; set; } = string.Empty;

    public string PayloadHash { get; set; } = string.Empty;

    public string TargetEntityType { get; set; } = string.Empty;

    public Guid TargetEntityId { get; set; }

    public DateTime FirstIngestedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastIngestedAt { get; set; } = DateTime.UtcNow;

    public int IngestCount { get; set; } = 1;
}
