using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAdminResourcesQuery
{
    public Guid? RegionId { get; init; }

    public ResourceType? Type { get; init; }

    public string? Name { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
