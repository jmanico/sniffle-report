using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class AdminResourceListDto
{
    public Guid Id { get; init; }

    public Guid RegionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public ResourceType Type { get; init; }

    public string Address { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Website { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
