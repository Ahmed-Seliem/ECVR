using System.ComponentModel.DataAnnotations;

namespace ECM.ReservationSystem.Models.Common;
public class Entity
{
    [Key]
    public int Id { get; set; }
    public DateTime? CreatedDate { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public long? ModifiedByUserId { get; set; }
    public bool? IsDelete { get; set; } = false;
    public DateTime? DeletedDate { get; set; }
    public long? DeleteByUserId { get; set; }
    public bool IsActive { get; set; }
}