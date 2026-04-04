using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class AlertFilters
{
    public AlertSeverity? Severity { get; init; }

    public string? Disease { get; init; }

    public AlertStatus? Status { get; init; }

    public DateTime? DateFrom { get; init; }

    public DateTime? DateTo { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}

public sealed class AdminAlertFilters
{
    public Guid? RegionId { get; init; }

    public string? Disease { get; init; }

    public AlertStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
