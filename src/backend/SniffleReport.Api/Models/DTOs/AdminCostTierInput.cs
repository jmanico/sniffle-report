using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminCostTierInput
{
    public CostTierType Type { get; init; }

    public decimal Price { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? Notes { get; init; }
}
