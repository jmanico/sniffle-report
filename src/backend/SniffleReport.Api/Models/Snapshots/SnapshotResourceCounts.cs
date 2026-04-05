namespace SniffleReport.Api.Models.Snapshots;

public sealed class SnapshotResourceCounts
{
    public int Clinic { get; set; }

    public int Pharmacy { get; set; }

    public int VaccinationSite { get; set; }

    public int Hospital { get; set; }

    public int Total { get; set; }
}
