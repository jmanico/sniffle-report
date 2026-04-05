namespace SniffleReport.Api.Models.Configuration;

public sealed class SnapshotOptions
{
    public const string SectionName = "Snapshot";

    public int RebuildIntervalMinutes { get; set; } = 5;

    public int TopAlertsCount { get; set; } = 10;

    public int TrendHighlightsCount { get; set; } = 5;

    public int PreventionHighlightsCount { get; set; } = 5;

    public int NewsHighlightsCount { get; set; } = 5;

    public int TrendWowWeeks { get; set; } = 2;
}
