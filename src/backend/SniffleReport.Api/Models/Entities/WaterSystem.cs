namespace SniffleReport.Api.Models.Entities;

public sealed class WaterSystem : EntityBase
{
    public string ExternalSourceId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SystemType { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? CountyServed { get; set; }

    public int? PopulationServed { get; set; }

    public ICollection<WaterSystemViolation> Violations { get; set; } = [];
}
