namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAdminPreventionGuidesQuery
{
    public Guid? RegionId { get; init; }

    public string? Disease { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
