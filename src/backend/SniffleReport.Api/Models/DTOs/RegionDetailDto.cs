using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class RegionDetailDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public RegionType Type { get; init; }

    public string State { get; init; } = string.Empty;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public int ChildCount { get; init; }

    public RegionParentDto? Parent { get; init; }
}
