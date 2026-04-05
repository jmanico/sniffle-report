using SniffleReport.Api.Models.Enums;
using ResourceType = SniffleReport.Api.Models.Enums.ResourceType;

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

    // Fields for LocalResourceEntry records
    public string? ResourceName { get; init; }

    public string? Address { get; init; }

    public string? Phone { get; init; }

    public string? Website { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public ResourceType? ResourceType { get; init; }
}
