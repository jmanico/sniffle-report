using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class NewsListDto
{
    public Guid Id { get; set; }

    public Guid RegionId { get; set; }

    public string Headline { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public FactCheckStatus? FactCheckStatus { get; set; }

    public string? SourceAttribution { get; set; }
}
