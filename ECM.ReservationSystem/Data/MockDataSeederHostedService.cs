using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.OpenIdSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECM.ReservationSystem.Data;

public class MockDataSeederHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MockDataOptions _options;
    private readonly ILogger<MockDataSeederHostedService> _logger;

    public MockDataSeederHostedService(
        IServiceProvider serviceProvider,
        IOptions<MockDataOptions> options,
        ILogger<MockDataSeederHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var hasData = await db.Cities.AnyAsync(cancellationToken);
        if (hasData && !_options.ForceReseed)
        {
            _logger.LogInformation("Mock data seeding skipped: data already exists.");
            return;
        }

        if (hasData && _options.ForceReseed)
        {
            await ClearSeededDataAsync(db, cancellationToken);
            _logger.LogInformation("Existing data cleared before reseeding.");
        }

        var now = DateTime.UtcNow;
        var currentYear = DateTime.UtcNow.Year;
        var random = new Random(20260302);

        var cities = new[]
        {
            new City { Name = "Alexandria", NameAr = "الإسكندرية", IsActive = true },
            new City { Name = "Marsa Matrouh", NameAr = "مرسى مطروح", IsActive = true },
            new City { Name = "Ras Sedr", NameAr = "رأس سدر", IsActive = true }
        };
        db.Cities.AddRange(cities);

        var unitTypes = new[]
        {
            new UnitType { Name = "Apartment", NameAr = "شقة", Description = "وحدة شاطئية", IsForManagement = false, IsActive = true },
            new UnitType { Name = "Villa", NameAr = "فيلا", Description = "وحدة عائلية", IsForManagement = false, IsActive = true },
            new UnitType { Name = "Chalet", NameAr = "شاليه", Description = "وحدة مصيفية", IsForManagement = false, IsActive = true }
        };
        db.UnitTypes.AddRange(unitTypes);
        await db.SaveChangesAsync(cancellationToken);

        var units = new List<Unit>();
        var floorTypes = new[] { FloorType.GroundFloor, FloorType.MiddleFloor, FloorType.TopFloor };
        var codeCounter = 1;
        foreach (var city in cities)
        {
            foreach (var type in unitTypes)
            {
                for (var i = 0; i < 3; i++)
                {
                    var floorType = floorTypes[i % floorTypes.Length];
                    units.Add(new Unit
                    {
                        Name = $"{type.Name} {city.Name} {i + 1}",
                        Code = $"U{codeCounter:0000}",
                        Description = $"Mock unit {codeCounter}",
                        DefaultCapacity = 6,
                        MaxCapacity = 6,
                        FloorType = floorType,
                        FloorNumber = floorType == FloorType.GroundFloor ? 0 : i + 1,
                        Year = currentYear,
                        IsActive = true,
                        CityId = city.Id,
                        UnitTypeId = type.Id
                    });
                    codeCounter++;
                }
            }
        }
        db.Units.AddRange(units);

        var availableDates = cities.Select(c => new AvailableDate
        {
            CityId = c.Id,
            AvailableFrom = new DateTime(currentYear, 6, 1),
            AvailableTo = new DateTime(currentYear, 9, 30),
            BookingOpenFrom = now.Date,
            BookingOpenTo = new DateTime(currentYear, 9, 25),
            IsBookingOpen = true,
            IsActive = true
        }).ToList();
        db.AvailableDates.AddRange(availableDates);

        var transportationCosts = cities.Select((c, idx) => new TransportationCost
        {
            CityId = c.Id,
            RoundTripCost = 350 + (idx * 120),
            EffectiveFrom = now.Date.AddMonths(-1),
            EffectiveTo = null,
            IsActive = true
        }).ToList();
        db.TransportationCosts.AddRange(transportationCosts);

        await db.SaveChangesAsync(cancellationToken);

        var pricings = units.Select(u => new Pricing
        {
            UnitId = u.Id,
            FloorType = u.FloorType,
            WeeklyRentDefaultCapacity = 6500 + random.Next(0, 3000),
            AdditionalPersonCost = 900,
            InsuranceAmount = 1200 + random.Next(0, 500),
            EffectiveFrom = now.Date.AddMonths(-1),
            EffectiveTo = null,
            IsActive = true
        }).ToList();
        db.Pricings.AddRange(pricings);

        if (_options.SeedReservations)
        {
            var sampleUnits = units.Take(8).ToList();
            var reservations = new List<Reservation>();
            for (var i = 0; i < sampleUnits.Count; i++)
            {
                var unit = sampleUnits[i];
                var checkIn = new DateTime(currentYear, 7, 1).AddDays(i * 7);
                var checkOut = checkIn.AddDays(6);
                reservations.Add(new Reservation
                {
                    EmployeeNumber = $"EMP{1000 + i}",
                    EmployeeName = $"Employee {i + 1}",
                    Year = currentYear.ToString(),
                    UnitId = unit.Id,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    NumberOfGuests = random.Next(2, 7),
                    WeeklyRent = 7000 + random.Next(0, 2500),
                    InsuranceAmount = 1200,
                    TransportationCost = i % 2 == 0 ? 350 : 0,
                    TotalAmount = 8200 + random.Next(0, 2600),
                    Status = i % 3 == 0 ? ReservationStatus.TemporaryHold : ReservationStatus.Confirmed,
                    PaymentDeadline = DateTime.UtcNow.AddHours(24),
                    IsTransportationRequired = i % 2 == 0,
                    Notes = "Mock reservation",
                    CaseSystemId = $"CASE-{Guid.NewGuid():N}".Substring(0, 12)
                });
            }

            db.Reservations.AddRange(reservations);
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Mock data seeded successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task ClearSeededDataAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        db.Reservations.RemoveRange(db.Reservations);
        db.Pricings.RemoveRange(db.Pricings);
        db.Units.RemoveRange(db.Units);
        db.TransportationCosts.RemoveRange(db.TransportationCosts);
        db.AvailableDates.RemoveRange(db.AvailableDates);
        db.UnitTypes.RemoveRange(db.UnitTypes);
        db.Cities.RemoveRange(db.Cities);
        await db.SaveChangesAsync(cancellationToken);
    }
}
