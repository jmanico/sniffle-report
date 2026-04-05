namespace SniffleReport.Api.Models.DTOs;

public sealed class NewsFilters
{
    public string? Headline { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;
}
