using ECM.ReservationSystem.Application.Abstractions;
using ECM.ReservationSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.ReservationSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IReservationRequestIngestionService, ReservationRequestIngestionService>();
        return services;
    }
}
