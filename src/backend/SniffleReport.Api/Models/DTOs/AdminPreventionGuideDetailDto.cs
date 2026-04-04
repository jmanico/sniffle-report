namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminPreventionGuideDetailDto
{
    public Guid Id { get; init; }

    public Guid RegionId { get; init; }

    public string Disease { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public bool IsDeleted { get; init; }

    public DateTime? DeletedAt { get; init; }

    public IReadOnlyList<CostTierDto> CostTiers { get; init; } = [];
}
