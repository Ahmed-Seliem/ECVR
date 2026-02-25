using Microsoft.EntityFrameworkCore;
using ECM.ReservationSystem.Models.Entities;

namespace ECM.ReservationSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<City> Cities { get; set; }
        public DbSet<UnitType> UnitTypes { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Pricing> Pricings { get; set; }
        public DbSet<TransportationCost> TransportationCosts { get; set; }
        public DbSet<AvailableDate> AvailableDates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // City Configuration
            modelBuilder.Entity<City>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Unit Configuration
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

            // Reservation Configuration
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Unit)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pricing Configuration
            modelBuilder.Entity<Pricing>()
                .HasOne(p => p.Unit)
                .WithMany(u => u.Pricings)
                .HasForeignKey(p => p.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // TransportationCost Configuration
            modelBuilder.Entity<TransportationCost>()
                .HasOne(tc => tc.City)
                .WithMany(c => c.TransportationCosts)
                .HasForeignKey(tc => tc.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            // AvailableDate Configuration
            modelBuilder.Entity<AvailableDate>()
                .HasOne(ad => ad.City)
                .WithMany()
                .HasForeignKey(ad => ad.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision
            modelBuilder.Entity<Pricing>()
                .Property(p => p.WeeklyRentDefaultCapacity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Pricing>()
                .Property(p => p.InsuranceAmount)
                .HasColumnType("decimal(18,2)");
        }
    }
}