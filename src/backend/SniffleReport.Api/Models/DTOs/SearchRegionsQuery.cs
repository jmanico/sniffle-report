namespace SniffleReport.Api.Models.DTOs;

public sealed class SearchRegionsQuery
{
    public string Q { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
