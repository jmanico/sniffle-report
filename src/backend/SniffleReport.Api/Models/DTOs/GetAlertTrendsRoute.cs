namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAlertTrendsRoute
{
    public Guid RegionId { get; init; }

    public Guid AlertId { get; init; }
}
