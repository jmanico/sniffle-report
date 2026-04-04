namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAdminNewsItemsQuery
{
    public Guid? RegionId { get; init; }

    public string? Headline { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
