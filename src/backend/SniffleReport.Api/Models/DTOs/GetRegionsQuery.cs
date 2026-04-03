using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetRegionsQuery
{
    public RegionType? Type { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
