namespace SniffleReport.Api.Models.Entities;

public sealed class PreventionGuide : SoftDeletableEntityBase
{
    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public string Disease { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CostTier> CostTiers { get; set; } = [];
}
