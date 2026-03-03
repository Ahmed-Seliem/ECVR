namespace ECM.ReservationSystem.Domain.Common;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    long? CreatedByUserId { get; set; }
    long? UpdatedByUserId { get; set; }
}
