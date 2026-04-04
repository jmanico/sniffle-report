namespace SniffleReport.Api.Models.DTOs;

public sealed class CreatePreventionGuideRequest
{
    public Guid RegionId { get; init; }

    public string Disease { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public IReadOnlyList<AdminCostTierInput> CostTiers { get; init; } = [];
}
