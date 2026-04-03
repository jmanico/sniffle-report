using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class Region : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public RegionType Type { get; set; }

    public string State { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public Guid? ParentId { get; set; }

    public Region? Parent { get; set; }

    public ICollection<Region> Children { get; set; } = [];

    public ICollection<HealthAlert> HealthAlerts { get; set; } = [];

    public ICollection<PreventionGuide> PreventionGuides { get; set; } = [];

    public ICollection<LocalResource> LocalResources { get; set; } = [];

    public ICollection<NewsItem> NewsItems { get; set; } = [];
}
