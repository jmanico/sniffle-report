namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAlertByIdRoute
{
    public Guid RegionId { get; init; }

    public Guid AlertId { get; init; }
}
