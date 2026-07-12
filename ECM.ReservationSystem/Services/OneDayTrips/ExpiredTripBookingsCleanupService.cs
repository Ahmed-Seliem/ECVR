using ECM.ReservationSystem.Services.OneDayTrips.Interfaces;

namespace ECM.ReservationSystem.Services.OneDayTrips
{
    // Auto-cancels One-Day-Trip bookings whose payment deadline has passed, releasing their tickets.
    public class ExpiredTripBookingsCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredTripBookingsCleanupService> _logger;

        public ExpiredTripBookingsCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ExpiredTripBookingsCleanupService> logger)
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
                    var bookingService = scope.ServiceProvider.GetRequiredService<ITripBookingService>();

                    var cancelled = await bookingService.CancelExpiredAsync();
                    if (cancelled > 0)
                    {
                        _logger.LogInformation(
                            "Auto-cancelled {count} expired One-Day-Trip bookings at {time}",
                            cancelled, DateTimeOffset.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired One-Day-Trip bookings");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
