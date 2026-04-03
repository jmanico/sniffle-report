using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class CostTier : EntityBase
{
    public Guid GuideId { get; set; }

    public PreventionGuide Guide { get; set; } = null!;

    public CostTierType Type { get; set; }

    public decimal Price { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
