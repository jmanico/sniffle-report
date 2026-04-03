namespace SniffleReport.Api.Models.DTOs;

public sealed class GetPreventionByIdRoute
{
    public Guid RegionId { get; init; }

    public Guid GuideId { get; init; }
}
