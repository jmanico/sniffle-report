using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminNewsItemDetailDto
{
    public Guid Id { get; init; }

    public Guid RegionId { get; init; }

    public string Headline { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string SourceUrl { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public bool IsDeleted { get; init; }

    public DateTime? DeletedAt { get; init; }

    public FactCheckStatus? FactCheckStatus { get; init; }

    public bool IsIngested { get; init; }

    public string? FeedSourceName { get; init; }

    public DateTime? LastFeedSync { get; init; }
}
