using ECM.ReservationSystem.Services.Interfaces;

namespace ECM.ReservationSystem.Services
{
    public class ExpiredHoldsCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredHoldsCleanupService> _logger;

        public ExpiredHoldsCleanupService(IServiceProvider serviceProvider, ILogger<ExpiredHoldsCleanupService> logger)
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
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

                    await reservationService.CleanupExpiredHoldsAsync();
                    _logger.LogInformation("Expired holds cleanup completed at {time}", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired holds");
                }

                // Run every 30 minutes
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}