using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminAlertDetailDto
{
    public Guid Id { get; init; }

    public Guid RegionId { get; init; }

    public string Disease { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public AlertSeverity Severity { get; init; }

    public int CaseCount { get; init; }

    public string SourceAttribution { get; init; } = string.Empty;

    public DateTime SourceDate { get; init; }

    public AlertStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public bool IsIngested { get; init; }

    public string? FeedSourceName { get; init; }

    public DateTime? LastFeedSync { get; init; }
}
