namespace SniffleReport.Api.Models.DTOs;

public sealed class TrendSeriesDto
{
    public Guid AlertId { get; init; }

    public Guid RegionId { get; init; }

    public string Disease { get; init; } = string.Empty;

    public string AlertTitle { get; init; } = string.Empty;

    public string SourceAttribution { get; init; } = string.Empty;

    public IReadOnlyList<TrendDataPointDto> DataPoints { get; init; } = [];
}
