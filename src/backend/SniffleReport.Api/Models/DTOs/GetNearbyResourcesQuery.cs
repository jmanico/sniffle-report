using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetNearbyResourcesQuery
{
    public double? Lat { get; init; }

    public double? Lng { get; init; }

    public double Radius { get; init; } = 10;

    public ResourceType? Type { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
