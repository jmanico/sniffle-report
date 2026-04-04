using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion;

public sealed class NormalizedFeedRecord
{
    public string ExternalSourceId { get; init; } = string.Empty;

    public string RawPayloadJson { get; init; } = string.Empty;

    public NormalizedRecordType RecordType { get; init; }

    public string? Disease { get; init; }

    public string? JurisdictionName { get; init; }

    public int? CaseCount { get; init; }

    public DateTime? DataDate { get; init; }

    public DateTime? SourceDate { get; init; }

    public string? Title { get; init; }

    public string? Summary { get; init; }

    public string? SourceUrl { get; init; }

    public string? SourceAttribution { get; init; }
}
