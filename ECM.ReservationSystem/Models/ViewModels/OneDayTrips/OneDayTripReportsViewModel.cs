using ECM.ReservationSystem.Domain.Entities.OneDayTrips;

namespace ECM.ReservationSystem.Models.ViewModels.OneDayTrips
{
    public class OneDayTripReportsViewModel
    {
        public int? LocationId { get; set; }
        public TripBookingType? BookingType { get; set; }
        public BookingStatus? Status { get; set; }

        public int TotalBookings { get; set; }
        public int TotalPersons { get; set; }
        public decimal GrandTotal { get; set; }

        public List<OneDayTripReportGroupViewModel> Groups { get; set; } = new();
    }

    public class OneDayTripReportGroupViewModel
    {
        public string LocationName { get; set; } = string.Empty;
        public DateTime TripDate { get; set; }
        public int BookingCount { get; set; }
        public int PersonCount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OneDayTripReportItemViewModel> Items { get; set; } = new();
    }

    public class OneDayTripReportItemViewModel
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
