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

        const int currentYear = 2026;

        var cities = new[]
        {
            new City { Name = "العين السخنة", NameAr = "العين السخنة", IsActive = true },
            new City { Name = "مرسى مطروح", NameAr = "مرسى مطروح", IsActive = true },
            new City { Name = "رأس سدر", NameAr = "رأس سدر", IsActive = true }
        };
        db.Cities.AddRange(cities);

        var unitTypes = new[]
        {
            new UnitType { Name = "Apartment", NameAr = "شقة", Description = "وحدة مناسبة للموظفين", IsForManagement = false, IsActive = true },
            new UnitType { Name = "Villa", NameAr = "فيلا", Description = "وحدة مناسبة للإدارة", IsForManagement = true, IsActive = true },
            new UnitType { Name = "Chalet", NameAr = "شاليه", Description = "وحدة مميزة للإدارة", IsForManagement = true, IsActive = true }
        };
        db.UnitTypes.AddRange(unitTypes);

        var unitFacades = new[]
        {
            new UnitFacade { Name = "Sea", NameAr = "بحري", IsActive = true },
            new UnitFacade { Name = "Garden", NameAr = "حديقة", IsActive = true },
            new UnitFacade { Name = "Pool", NameAr = "حمام سباحة", IsActive = true }
        };
        db.UnitFacades.AddRange(unitFacades);
        await db.SaveChangesAsync(cancellationToken);

        var units = new List<Unit>
        {
            new()
            {
                Name = "شقة الياسمين",
                Code = "101",
                Description = "شقة عائلية بإطلالة مفتوحة",
                DefaultCapacity = 4,
                MaxCapacity = 4,
                RoomCount = 2,
                UnitFacadeId = unitFacades[0].Id,
                FloorType = FloorType.MiddleFloor,
                FloorNumber = 2,
                Year = currentYear,
                IsActive = true,
                IsForPensioners = false,
                CityId = cities[0].Id,
                UnitTypeId = unitTypes[0].Id
            },
            new()
            {
                Name = "شقة الروضة",
                Code = "202",
                Description = "شقة هادئة قريبة من البحر",
                DefaultCapacity = 4,
                MaxCapacity = 4,
                RoomCount = 2,
                UnitFacadeId = unitFacades[1].Id,
                FloorType = FloorType.TopFloor,
                FloorNumber = 3,
                Year = currentYear,
                IsActive = true,
                IsForPensioners = true,
                CityId = cities[1].Id,
                UnitTypeId = unitTypes[0].Id
            },
            new()
            {
                Name = "فيلا النخيل",
                Code = "V1",
                Description = "فيلا للإدارة بحديقة خاصة",
                DefaultCapacity = 6,
                MaxCapacity = 6,
                RoomCount = 4,
                UnitFacadeId = unitFacades[1].Id,
                FloorType = FloorType.GroundFloor,
                FloorNumber = 0,
                Year = currentYear,
                IsActive = true,
                IsForPensioners = false,
                CityId = cities[0].Id,
                UnitTypeId = unitTypes[1].Id
            },
            new()
            {
                Name = "فيلا المرجان",
                Code = "V2",
                Description = "فيلا واسعة قريبة من الشاطئ",
                DefaultCapacity = 6,
                MaxCapacity = 6,
                RoomCount = 4,
                UnitFacadeId = unitFacades[0].Id,
                FloorType = FloorType.GroundFloor,
                FloorNumber = 0,
                Year = currentYear,
                IsActive = true,
                IsForPensioners = false,
                CityId = cities[2].Id,
                UnitTypeId = unitTypes[1].Id
            },
            new()
            {
                Name = "شاليه النسيم",
                Code = "C1",
                Description = "شاليه مميز للإدارة",
                DefaultCapacity = 5,
                MaxCapacity = 5,
                RoomCount = 3,
                UnitFacadeId = unitFacades[2].Id,
                FloorType = FloorType.MiddleFloor,
                FloorNumber = 1,
                Year = currentYear,
                IsActive = true,
                IsForPensioners = false,
                CityId = cities[1].Id,
                UnitTypeId = unitTypes[2].Id
            },
            new()
            {
                Name = "شاليه اللوتس",
                Code = "C2",
                Description = "شاليه بإطلالة مباشرة",
                DefaultCapacity = 5,
                MaxCapacity = 5,
                RoomCount = 3,
                UnitFacadeId = unitFacades[0].Id,
                FloorType = FloorType.TopFloor,
                FloorNumber = 2,
                Year = currentYear,
                IsActive = true,
                IsForPensioners = true,
                CityId = cities[2].Id,
                UnitTypeId = unitTypes[2].Id
            }
        };
        db.Units.AddRange(units);
        await db.SaveChangesAsync(cancellationToken);

        var availableDates = cities.Select(city => new AvailableDate
        {
            CityId = city.Id,
            AvailableFrom = new DateTime(currentYear, 6, 1),
            AvailableTo = new DateTime(currentYear, 9, 30),
            BookingOpenFrom = new DateTime(currentYear, 5, 1),
            BookingOpenTo = new DateTime(currentYear, 9, 25),
            IsBookingOpen = true,
            IsActive = true
        }).ToList();
        db.AvailableDates.AddRange(availableDates);

        var pricings = new List<Pricing>();
        foreach (var unit in units)
        {
            pricings.Add(new Pricing
            {
                UnitId = unit.Id,
                FloorType = unit.FloorType,
                WeeklyRentDefaultCapacity = unit.UnitTypeId == unitTypes[0].Id ? 5200 : unit.UnitTypeId == unitTypes[1].Id ? 8800 : 7600,
                AdditionalPersonCost = 0,
                InsuranceAmount = 1500,
                TransportationCostPerPerson = 180,
                EffectiveFrom = new DateTime(currentYear, 1, 1),
                EffectiveTo = null,
                IsActive = true
            });
        }
        db.Pricings.AddRange(pricings);
        await db.SaveChangesAsync(cancellationToken);

        var scheduleSlots = new List<UnitScheduleSlot>();
        foreach (var unit in units)
        {
            foreach (var slot in BuildSlotsForSummer(currentYear))
            {
                scheduleSlots.Add(new UnitScheduleSlot
                {
                    UnitId = unit.Id,
                    Year = currentYear,
                    Name = $"{slot.Name} - {unit.Name}",
                    SlotStartDate = slot.StartDate,
                    SlotEndDate = slot.EndDate,
                    IsActive = true,
                    Notes = "جدول صيف 2026"
                });
            }
        }
        db.UnitScheduleSlots.AddRange(scheduleSlots);
        await db.SaveChangesAsync(cancellationToken);

        if (_options.SeedReservations)
        {
            var targetSlots = await db.UnitScheduleSlots
                .Include(s => s.Unit)
                .OrderBy(s => s.UnitId)
                .ThenBy(s => s.SlotStartDate)
                .ToListAsync(cancellationToken);

            var reservations = new List<Reservation>();
            var bookedSlots = targetSlots
                .Where((_, index) => index % 5 == 0)
                .Take(6)
                .ToList();

            var employeeNames = new[]
            {
                "أحمد محمد",
                "مها علي",
                "سارة حسن",
                "خالد إبراهيم",
                "ندى سمير",
                "محمد شريف"
            };

            for (var i = 0; i < bookedSlots.Count; i++)
            {
                var slot = bookedSlots[i];
                reservations.Add(new Reservation
                {
                    EmployeeNumber = $"EMP-{100 + i}",
                    EmployeeName = employeeNames[i],
                    Year = currentYear.ToString(),
                    UnitId = slot.UnitId,
                    CheckInDate = slot.SlotStartDate,
                    CheckOutDate = slot.SlotEndDate,
                    NumberOfGuests = 4,
                    WeeklyRent = 0,
                    InsuranceAmount = 1500,
                    TransportationCost = 720,
                    TotalAmount = 0,
                    Status = i % 2 == 0 ? ReservationStatus.Confirmed : ReservationStatus.Paid,
                    PaymentDeadline = DateTime.UtcNow.AddHours(24),
                    IsTransportationRequired = true,
                    Notes = $"حجز تجريبي على {slot.Name}",
                    CaseSystemId = $"CASE-2026-{i + 1:000}"
                });
            }

            foreach (var reservation in reservations)
            {
                var pricing = pricings.First(p => p.UnitId == reservation.UnitId);
                reservation.WeeklyRent = pricing.WeeklyRentDefaultCapacity;
                reservation.TotalAmount = reservation.WeeklyRent + reservation.InsuranceAmount + reservation.TransportationCost;
            }

            db.Reservations.AddRange(reservations);
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Mock data seeded successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IEnumerable<(DateTime StartDate, DateTime EndDate, string Name)> BuildSlotsForSummer(int year)
    {
        var slots = new List<(DateTime StartDate, DateTime EndDate, string Name)>();
        var startDate = GetFirstFridayOnOrAfter(new DateTime(year, 6, 1));
        var endDate = new DateTime(year, 9, 30);
        var slotCounter = 1;

        for (var current = startDate; current <= endDate; current = current.AddDays(7))
        {
            var slotEnd = current.AddDays(7);
            if (slotEnd > endDate)
            {
                break;
            }

            slots.Add((current, slotEnd, $"الأسبوع {slotCounter:00}"));
            slotCounter++;
        }

        return slots;
    }

    private static DateTime GetFirstFridayOnOrAfter(DateTime date)
    {
        while (date.DayOfWeek != DayOfWeek.Friday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static async Task ClearSeededDataAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        db.Reservations.RemoveRange(db.Reservations);
        db.UnitScheduleSlots.RemoveRange(db.UnitScheduleSlots);
        db.Pricings.RemoveRange(db.Pricings);
        db.Units.RemoveRange(db.Units);
        db.UnitFacades.RemoveRange(db.UnitFacades);
        db.AvailableDates.RemoveRange(db.AvailableDates);
        db.UnitTypes.RemoveRange(db.UnitTypes);
        db.Cities.RemoveRange(db.Cities);
        await db.SaveChangesAsync(cancellationToken);
    }
}
