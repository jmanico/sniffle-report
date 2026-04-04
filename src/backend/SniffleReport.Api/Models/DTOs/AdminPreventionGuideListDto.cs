namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminPreventionGuideListDto
{
    public Guid Id { get; init; }

    public Guid RegionId { get; init; }

    public string Disease { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public bool IsDeleted { get; init; }

    public IReadOnlyList<CostTierDto> CostTiers { get; init; } = [];
}
