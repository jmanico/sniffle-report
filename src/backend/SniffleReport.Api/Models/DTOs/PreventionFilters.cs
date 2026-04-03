namespace SniffleReport.Api.Models.DTOs;

public sealed class PreventionFilters
{
    public string? Disease { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
