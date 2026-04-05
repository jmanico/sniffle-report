namespace SniffleReport.Api.Models.DTOs;

public sealed class RegionStatusDto
{
    public Guid RegionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? ParentName { get; set; }

    public DateTime? ComputedAt { get; set; }

    public int PublishedAlertCount { get; set; }

    public int ResourceTotal { get; set; }
}
