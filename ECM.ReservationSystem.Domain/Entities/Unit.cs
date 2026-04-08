using System.ComponentModel.DataAnnotations;
using ECM.ReservationSystem.Domain.Common;

namespace ECM.ReservationSystem.Domain.Entities;

public class Unit : AuditableEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }
    public string? Description { get; set; }

    public int DefaultCapacity { get; set; } = 6;
    public int MaxCapacity { get; set; } = 10;
    public int RoomCount { get; set; } = 1;
    public FloorType FloorType { get; set; }
    public int FloorNumber { get; set; }
    public int Year { get; set; } = DateTime.Now.Year;
    public bool IsActive { get; set; } = true;
    public bool IsForPensioners { get; set; }

    public int CityId { get; set; }
    public int UnitTypeId { get; set; }
    public int? UnitFacadeId { get; set; }

    public City? City { get; set; }
    public UnitType? UnitType { get; set; }
    public UnitFacade? UnitFacade { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Pricing> Pricings { get; set; } = new List<Pricing>();
    public ICollection<UnitScheduleSlot> ScheduleSlots { get; set; } = new List<UnitScheduleSlot>();
}

public enum FloorType
{
    GroundFloor = 1,
    MiddleFloor = 2,
    TopFloor = 3
}
