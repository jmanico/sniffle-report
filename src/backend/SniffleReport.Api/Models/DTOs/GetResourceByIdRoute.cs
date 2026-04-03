namespace SniffleReport.Api.Models.DTOs;

public sealed class GetResourceByIdRoute
{
    public Guid RegionId { get; init; }

    public Guid ResourceId { get; init; }
}
