namespace SniffleReport.Api.Models.DTOs;

public sealed class DiseaseTrendDto
{
    public DateTime Date { get; init; }

    public int CaseCount { get; init; }

    public string Source { get; init; } = string.Empty;

    public DateTime SourceDate { get; init; }

    public string? Notes { get; init; }
}
