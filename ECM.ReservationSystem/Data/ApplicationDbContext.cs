using System.Security.Claims;
using ECM.ReservationSystem.Domain.Common;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Domain.Entities.OneDayTrips;
using ECM.ReservationSystem.Domain.Entities.HotelTrips;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<City> Cities { get; set; }
    public DbSet<UnitType> UnitTypes { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<UnitFacade> UnitFacades { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationHold> ReservationHolds { get; set; }
    public DbSet<ReservationSubmissionAttempt> ReservationSubmissionAttempts { get; set; }
    public DbSet<ReservationType> ReservationTypes { get; set; }
    public DbSet<Pricing> Pricings { get; set; }
    public DbSet<TransportationCost> TransportationCosts { get; set; }
    public DbSet<TransportQuota> TransportQuotas { get; set; }
    public DbSet<AvailableDate> AvailableDates { get; set; }
    public DbSet<UnitScheduleSlot> UnitScheduleSlots { get; set; }

    // One Day Trips module (isolated from the legacy reservation business)
    public DbSet<TripLocation> TripLocations { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<TripBooking> TripBookings { get; set; }

    // Hotel Trips module (isolated from OneDayTrips and the legacy business)
    public DbSet<HotelCity> HotelCities { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<HotelTrip> HotelTrips { get; set; }
    public DbSet<HotelTripBooking> HotelTripBookings { get; set; }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<City>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<UnitFacade>()
            .HasIndex(f => f.Name)
            .IsUnique();

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.City)
            .WithMany(c => c.Units)
            .HasForeignKey(u => u.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.UnitType)
            .WithMany(ut => ut.Units)
            .HasForeignKey(u => u.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.UnitFacade)
            .WithMany(f => f.Units)
            .HasForeignKey(u => u.UnitFacadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Unit)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReservationHold>()
            .HasOne(r => r.Unit)
            .WithMany()
            .HasForeignKey(r => r.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.DocumentId)
            .HasFilter("[DocumentId] IS NOT NULL");

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.UnitId, r.CheckInDate, r.CheckOutDate });

        modelBuilder.Entity<ReservationHold>()
            .HasIndex(h => h.HoldToken)
            .IsUnique();

        modelBuilder.Entity<ReservationHold>()
            .HasIndex(h => new { h.UnitId, h.CheckInDate, h.CheckOutDate });

        modelBuilder.Entity<ReservationHold>()
            .HasIndex(h => new { h.EmployeeNumber, h.CheckInDate, h.CheckOutDate });

        modelBuilder.Entity<ReservationSubmissionAttempt>()
            .HasIndex(a => a.DocumentId);

        modelBuilder.Entity<ReservationSubmissionAttempt>()
            .HasIndex(a => new { a.UnitId, a.CheckInDate, a.CheckOutDate });

        modelBuilder.Entity<Pricing>()
            .HasOne(p => p.Unit)
            .WithMany(u => u.Pricings)
            .HasForeignKey(p => p.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UnitScheduleSlot>()
            .HasOne(s => s.Unit)
            .WithMany(u => u.ScheduleSlots)
            .HasForeignKey(s => s.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransportationCost>()
            .HasOne(tc => tc.City)
            .WithMany(c => c.TransportationCosts)
            .HasForeignKey(tc => tc.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TransportQuota>()
            .HasOne(tq => tq.City)
            .WithMany(c => c.TransportQuotas)
            .HasForeignKey(tq => tq.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AvailableDate>()
            .HasOne(ad => ad.City)
            .WithMany()
            .HasForeignKey(ad => ad.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pricing>()
            .Property(p => p.WeeklyRentDefaultCapacity)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Pricing>()
            .Property(p => p.InsuranceAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Pricing>()
            .Property(p => p.TransportationCostPerPerson)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<UnitScheduleSlot>()
            .HasIndex(s => new { s.UnitId, s.SlotStartDate, s.SlotEndDate })
            .IsUnique();

        modelBuilder.Entity<TransportQuota>()
            .HasIndex(tq => new { tq.CityId, tq.SeasonYear })
            .IsUnique();

        // ===== One Day Trips module (isolated) =====
        modelBuilder.Entity<TripLocation>()
            .HasIndex(l => l.Name)
            .IsUnique();

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.TripLocation)
            .WithMany(l => l.Trips)
            .HasForeignKey(t => t.TripLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasIndex(t => new { t.TripLocationId, t.TripDate });

        modelBuilder.Entity<TripBooking>()
            .HasOne(b => b.Trip)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TripBooking>()
            .HasIndex(b => b.TripId);

        modelBuilder.Entity<TripBooking>()
            .HasIndex(b => new { b.EmployeeNumber, b.TripId });

        // DB-level guard for booking business rules (extra layer beside the service validation)
        modelBuilder.Entity<TripBooking>()
            .ToTable(t =>
            {
                t.HasCheckConstraint("CK_TripBookings_AdultsCount_Min", "[AdultsCount] >= 1");
                t.HasCheckConstraint("CK_TripBookings_ChildrenCount_NonNegative", "[ChildrenCount] >= 0");
                t.HasCheckConstraint("CK_TripBookings_CompanionsCount_NonNegative", "[CompanionsCount] >= 0");
                t.HasCheckConstraint("CK_TripBookings_TotalGuests_Max", "[AdultsCount] + [ChildrenCount] + [CompanionsCount] <= 5");
            });

        // ===== Hotel Trips module (isolated) =====
        modelBuilder.Entity<HotelCity>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.HotelCity)
            .WithMany(c => c.Hotels)
            .HasForeignKey(h => h.HotelCityId)
            .OnDelete(DeleteBehavior.Restrict);

        // One ticket pool per hotel (1:1).
        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.Ticket)
            .WithOne(t => t.Hotel)
            .HasForeignKey<Ticket>(t => t.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HotelTrip>()
            .HasOne(t => t.Hotel)
            .WithMany(h => h.HotelTrips)
            .HasForeignKey(t => t.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HotelTrip>()
            .HasIndex(t => new { t.HotelId, t.StartDate, t.EndDate });

        modelBuilder.Entity<HotelTripBooking>()
            .HasOne(b => b.HotelTrip)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.HotelTripId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HotelTripBooking>()
            .HasIndex(b => b.HotelTripId);

        modelBuilder.Entity<HotelTripBooking>()
            .HasIndex(b => new { b.EmployeeNumber, b.HotelTripId });

        // DB-level guard for booking business rules (extra layer beside the service validation)
        modelBuilder.Entity<HotelTripBooking>()
            .ToTable(t =>
            {
                t.HasCheckConstraint("CK_HotelTripBookings_AdultsCount_Min", "[AdultsCount] >= 1");
                t.HasCheckConstraint("CK_HotelTripBookings_ChildrenCount_NonNegative", "[ChildrenCount] >= 0");
                t.HasCheckConstraint("CK_HotelTripBookings_CompanionsCount_NonNegative", "[CompanionsCount] >= 0");
                t.HasCheckConstraint("CK_HotelTripBookings_TotalGuests_Max", "[AdultsCount] + [ChildrenCount] + [CompanionsCount] <= 5");
            });
    }

    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;
        var currentUserId = GetCurrentUserId();

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.UpdatedAt = null;
                entry.Entity.CreatedByUserId ??= currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditableEntity.CreatedByUserId)).IsModified = false;

                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedByUserId = currentUserId;
            }
        }
    }

    private long? GetCurrentUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var possibleValues = new[]
        {
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            principal.FindFirstValue("Id"),
            principal.FindFirstValue("UserId")
        };

        foreach (var value in possibleValues)
        {
            if (!string.IsNullOrWhiteSpace(value) && long.TryParse(value, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
