using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminNewsItemListDto
{
    public Guid Id { get; init; }

    public Guid RegionId { get; init; }

    public string Headline { get; init; } = string.Empty;

    public string SourceUrl { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public bool IsDeleted { get; init; }

    public FactCheckStatus? FactCheckStatus { get; init; }
}
