using ECM.ReservationSystem.Services.HotelTrips.Interfaces;

namespace ECM.ReservationSystem.Services.HotelTrips
{
    // Auto-cancels Hotel-Trip bookings whose payment deadline has passed, releasing their tickets.
    public class ExpiredHotelTripBookingsCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredHotelTripBookingsCleanupService> _logger;

        public ExpiredHotelTripBookingsCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ExpiredHotelTripBookingsCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IHotelTripBookingService>();

                    var cancelled = await bookingService.CancelExpiredAsync();
                    if (cancelled > 0)
                    {
                        _logger.LogInformation(
                            "Auto-cancelled {count} expired Hotel-Trip bookings at {time}",
                            cancelled, DateTimeOffset.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired Hotel-Trip bookings");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
