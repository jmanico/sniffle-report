namespace SniffleReport.Api.Models.DTOs;

public sealed class GetNewsQuery
{
    public string? Headline { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;
}
