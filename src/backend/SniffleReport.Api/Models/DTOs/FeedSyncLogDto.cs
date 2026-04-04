using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class FeedSyncLogDto
{
    public Guid Id { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public FeedSyncStatus Status { get; init; }

    public int RecordsFetched { get; init; }

    public int RecordsCreated { get; init; }

    public int RecordsUpdated { get; init; }

    public int RecordsSkippedDuplicate { get; init; }

    public int RecordsSkippedUnmappable { get; init; }

    public int AlertsPromoted { get; init; }

    public string? ErrorMessage { get; init; }
}
