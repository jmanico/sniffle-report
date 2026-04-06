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

    // Fields for ShortageAreaDesignation records
    public string? AreaName { get; init; }

    public string? Discipline { get; init; }

    public string? DesignationType { get; init; }

    public string? DesignationStatus { get; init; }

    public string? PopulationGroup { get; init; }

    public int? HpsaScore { get; init; }

    public decimal? PopulationToProviderRatio { get; init; }

    // Fields for DrinkingWaterViolation records
    public string? ParentExternalSourceId { get; init; }

    public string? WaterSystemName { get; init; }

    public string? WaterSystemType { get; init; }

    public string? WaterSystemAddress { get; init; }

    public string? WaterSystemCity { get; init; }

    public string? WaterSystemState { get; init; }

    public string? WaterSystemPostalCode { get; init; }

    public string? CountyServed { get; init; }

    public int? PopulationServed { get; init; }

    public string? ViolationCategory { get; init; }

    public string? RuleName { get; init; }

    public string? ContaminantName { get; init; }

    public bool? IsOpenViolation { get; init; }

    public DateTime? IdentifiedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }
}
