namespace SniffleReport.Api.Models.DTOs;

public sealed class FeedStatusDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public string? LastSyncStatus { get; set; }

    public DateTime? LastSyncCompletedAt { get; set; }

    public int ConsecutiveFailureCount { get; set; }

    public int? LastRecordsCreated { get; set; }

    public int? LastRecordsFetched { get; set; }

    public int? LastRecordsSkippedUnmappable { get; set; }

    public string? LastSyncError { get; set; }
}
