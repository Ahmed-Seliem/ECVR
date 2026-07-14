using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities.HotelTrips;
using ECM.ReservationSystem.Models.DTOs.HotelTrips;
using ECM.ReservationSystem.Services.HotelTrips.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECM.ReservationSystem.Services.HotelTrips.Implementations
{
    public class HotelService : IHotelService
    {
        private readonly ApplicationDbContext _context;

        public HotelService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<HotelResponseDto>> GetAllAsync(int? cityId = null)
        {
            var query = _context.Hotels
                .Include(h => h.HotelCity)
                .Include(h => h.Ticket)
                .Include(h => h.HotelTrips)
                .AsNoTracking()
                .AsQueryable();

            if (cityId.HasValue)
            {
                query = query.Where(h => h.HotelCityId == cityId.Value);
            }

            var hotels = await query.OrderBy(h => h.Name).ToListAsync();

            var usedByHotel = await GetUsedTicketsByHotelAsync();

            return hotels
                .Select(h => MapToDto(h, usedByHotel.TryGetValue(h.Id, out var used) ? used : (0, 0)))
                .ToList();
        }

        public async Task<HotelResponseDto?> GetByIdAsync(int id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.HotelCity)
                .Include(h => h.Ticket)
                .Include(h => h.HotelTrips)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel is null)
            {
                return null;
            }

            var used = (
                await GetUsedTicketsAsync(id, HotelBookingType.Employee),
                await GetUsedTicketsAsync(id, HotelBookingType.Pension));
            return MapToDto(hotel, used);
        }

        public async Task<int> GetRemainingTicketsAsync(int hotelId, HotelBookingType bookingType)
        {
            var ticket = await _context.Tickets
                .Where(t => t.HotelId == hotelId)
                .Select(t => new { t.EmployeeQuantity, t.PensionQuantity })
                .FirstOrDefaultAsync();

            var pool = ticket is null
                ? 0
                : (bookingType == HotelBookingType.Pension ? ticket.PensionQuantity : ticket.EmployeeQuantity);

            var used = await GetUsedTicketsAsync(hotelId, bookingType);
            return Math.Max(0, pool - used);
        }

        public async Task<HotelResponseDto> CreateAsync(HotelRequestDto request)
        {
            var cityExists = await _context.HotelCities.AnyAsync(c => c.Id == request.HotelCityId);
            if (!cityExists)
            {
                throw new InvalidOperationException("المدينة المحددة غير موجودة.");
            }

            var hotel = new Hotel
            {
                HotelCityId = request.HotelCityId!.Value,
                Name = request.Name,
                NameAr = request.NameAr,
                AdultTicketPrice = request.AdultTicketPrice!.Value,
                ChildTicketPrice = request.ChildTicketPrice!.Value,
                CompanionTicketPrice = request.CompanionTicketPrice!.Value,
                IsActive = request.IsActive,
                Ticket = new Ticket
                {
                    EmployeeQuantity = request.EmployeeTicketQuantity,
                    PensionQuantity = request.PensionTicketQuantity
                }
            };

            _context.Hotels.Add(hotel);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(hotel.Id) ?? MapToDto(hotel, (0, 0));
        }

        public async Task<bool> UpdateAsync(int id, HotelRequestDto request)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Ticket)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel is null)
            {
                return false;
            }

            var cityExists = await _context.HotelCities.AnyAsync(c => c.Id == request.HotelCityId);
            if (!cityExists)
            {
                throw new InvalidOperationException("المدينة المحددة غير موجودة.");
            }

            hotel.HotelCityId = request.HotelCityId!.Value;
            hotel.Name = request.Name;
            hotel.NameAr = request.NameAr;
            hotel.AdultTicketPrice = request.AdultTicketPrice!.Value;
            hotel.ChildTicketPrice = request.ChildTicketPrice!.Value;
            hotel.CompanionTicketPrice = request.CompanionTicketPrice!.Value;
            hotel.IsActive = request.IsActive;

            if (hotel.Ticket is null)
            {
                hotel.Ticket = new Ticket
                {
                    HotelId = hotel.Id,
                    EmployeeQuantity = request.EmployeeTicketQuantity,
                    PensionQuantity = request.PensionTicketQuantity
                };
                _context.Tickets.Add(hotel.Ticket);
            }
            else
            {
                hotel.Ticket.EmployeeQuantity = request.EmployeeTicketQuantity;
                hotel.Ticket.PensionQuantity = request.PensionTicketQuantity;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == id);
            if (hotel is null)
            {
                return false;
            }

            var hasTrips = await _context.HotelTrips.AnyAsync(t => t.HotelId == id);
            if (hasTrips)
            {
                throw new InvalidOperationException("لا يمكن حذف الفندق لأنه مرتبط برحلات.");
            }

            _context.Hotels.Remove(hotel); // Ticket is removed via cascade
            await _context.SaveChangesAsync();
            return true;
        }

        // Tickets consumed by active bookings (confirmed, or pending-payment not yet expired) of the given type.
        private async Task<int> GetUsedTicketsAsync(int hotelId, HotelBookingType bookingType)
        {
            var now = DateTime.Now;
            return await _context.HotelTripBookings
                .Where(b => b.HotelTrip.HotelId == hotelId
                            && b.BookingType == bookingType
                            && (b.Status == HotelBookingStatus.Confirmed
                                || (b.Status == HotelBookingStatus.PendingPayment
                                    && b.PaymentDeadline != null
                                    && b.PaymentDeadline > now)))
                .SumAsync(b => b.AdultsCount + b.ChildrenCount + b.CompanionsCount);
        }

        private async Task<Dictionary<int, (int Employee, int Pension)>> GetUsedTicketsByHotelAsync()
        {
            var now = DateTime.Now;
            var grouped = await _context.HotelTripBookings
                .Where(b => b.Status == HotelBookingStatus.Confirmed
                            || (b.Status == HotelBookingStatus.PendingPayment
                                && b.PaymentDeadline != null
                                && b.PaymentDeadline > now))
                .GroupBy(b => new { b.HotelTrip.HotelId, b.BookingType })
                .Select(g => new
                {
                    g.Key.HotelId,
                    g.Key.BookingType,
                    Used = g.Sum(x => x.AdultsCount + x.ChildrenCount + x.CompanionsCount)
                })
                .ToListAsync();

            var dict = new Dictionary<int, (int Employee, int Pension)>();
            foreach (var row in grouped)
            {
                dict.TryGetValue(row.HotelId, out var current);
                if (row.BookingType == HotelBookingType.Pension)
                {
                    current.Pension += row.Used;
                }
                else
                {
                    current.Employee += row.Used;
                }
                dict[row.HotelId] = current;
            }

            return dict;
        }

        private static HotelResponseDto MapToDto(Hotel hotel, (int Employee, int Pension) used)
        {
            var employeeQty = hotel.Ticket?.EmployeeQuantity ?? 0;
            var pensionQty = hotel.Ticket?.PensionQuantity ?? 0;
            return new HotelResponseDto
            {
                Id = hotel.Id,
                HotelCityId = hotel.HotelCityId,
                CityName = hotel.HotelCity is null
                    ? string.Empty
                    : (string.IsNullOrWhiteSpace(hotel.HotelCity.NameAr) ? hotel.HotelCity.Name : hotel.HotelCity.NameAr),
                Name = hotel.Name,
                NameAr = hotel.NameAr,
                AdultTicketPrice = hotel.AdultTicketPrice,
                ChildTicketPrice = hotel.ChildTicketPrice,
                CompanionTicketPrice = hotel.CompanionTicketPrice,
                EmployeeTicketQuantity = employeeQty,
                PensionTicketQuantity = pensionQty,
                EmployeeRemaining = Math.Max(0, employeeQty - used.Employee),
                PensionRemaining = Math.Max(0, pensionQty - used.Pension),
                IsActive = hotel.IsActive,
                TripsCount = hotel.HotelTrips?.Count ?? 0,
                CreatedAt = hotel.CreatedAt
            };
        }
    }
}
