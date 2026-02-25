using ECM.ReservationSystem.Application.Abstractions;
using ECM.ReservationSystem.Infrastructure.Integrations.CaseWorkflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.ReservationSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CaseWorkflowOptions>(configuration.GetSection(CaseWorkflowOptions.SectionName));

        services.AddHttpClient<ICaseWorkflowClient, CaseWorkflowClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<CaseWorkflowOptions>>()
                .Value;

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }
        });

        return services;
    }
}
