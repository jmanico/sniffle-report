using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class GetAdminAlertsQuery
{
    public Guid? RegionId { get; init; }

    public string? Disease { get; init; }

    public AlertStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
