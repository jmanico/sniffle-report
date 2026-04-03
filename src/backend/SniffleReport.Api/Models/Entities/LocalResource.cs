using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class LocalResource : EntityBase
{
    public Guid RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ResourceType Type { get; set; }

    public string Address { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string HoursJson { get; set; } = "{}";

    public string ServicesJson { get; set; } = "[]";
}
