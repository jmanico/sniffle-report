namespace SniffleReport.Api.Models.DTOs;

public sealed class UpdateNewsItemRequest
{
    public Guid RegionId { get; init; }

    public string Headline { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string SourceUrl { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }
}
