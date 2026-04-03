namespace SniffleReport.Api.Models.DTOs;

public sealed class TrendFilters
{
    public string? Disease { get; init; }

    public DateTime? DateFrom { get; init; }

    public DateTime? DateTo { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
