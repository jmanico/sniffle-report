using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class UpdateResourceRequest
{
    public Guid RegionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public ResourceType Type { get; init; }

    public string Address { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Website { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public ResourceHoursDto Hours { get; init; } = new();

    public IReadOnlyList<string> Services { get; init; } = [];
}
