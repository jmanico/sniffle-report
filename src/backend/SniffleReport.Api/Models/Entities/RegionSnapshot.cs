namespace SniffleReport.Api.Models.Entities;

public sealed class RegionSnapshot : EntityBase
{
    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public DateTime ComputedAt { get; set; }

    public int PublishedAlertCount { get; set; }

    public string TopAlertsJson { get; set; } = "[]";

    public string TrendHighlightsJson { get; set; } = "[]";

    public string ResourceCountsJson { get; set; } = "{}";

    public string AccessSignalsJson { get; set; } = "[]";

    public string EnvironmentalSignalsJson { get; set; } = "[]";

    public string PreventionHighlightsJson { get; set; } = "[]";

    public string NewsHighlightsJson { get; set; } = "[]";
}
