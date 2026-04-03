namespace SniffleReport.Api.Models.Entities;

public abstract class SoftDeletableEntityBase : EntityBase
{
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
}
