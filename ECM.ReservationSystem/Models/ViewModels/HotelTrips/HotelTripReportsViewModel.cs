using ECM.ReservationSystem.Domain.Entities.HotelTrips;

namespace ECM.ReservationSystem.Models.ViewModels.HotelTrips
{
    public class HotelTripReportsViewModel
    {
        public int? HotelId { get; set; }
        public HotelBookingType? BookingType { get; set; }
        public HotelBookingStatus? Status { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }

        public int TotalBookings { get; set; }
        public int TotalPersons { get; set; }
        public decimal GrandTotal { get; set; }

        public List<HotelTripReportGroupViewModel> Groups { get; set; } = new();
    }

    public class HotelTripReportGroupViewModel
    {
        public string HotelName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int BookingCount { get; set; }
        public int PersonCount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<HotelTripReportItemViewModel> Items { get; set; } = new();
    }

    public class HotelTripReportItemViewModel
    {
        public int BookingId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int AdultsCount { get; set; }
        public int ChildrenCount { get; set; }
        public int CompanionsCount { get; set; }
        public int TotalPersons { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingTypeText { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
    }
}
