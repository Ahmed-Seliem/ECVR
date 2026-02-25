using System.Net.Http.Json;
using ECM.ReservationSystem.Application.Abstractions;
using ECM.ReservationSystem.Application.Contracts.CaseWorkflow;
using Microsoft.Extensions.Options;

namespace ECM.ReservationSystem.Infrastructure.Integrations.CaseWorkflow;

public sealed class CaseWorkflowClient(
    HttpClient httpClient,
    IOptions<CaseWorkflowOptions> options) : ICaseWorkflowClient
{
    private readonly CaseWorkflowOptions _options = options.Value;

    public async Task<CaseReservationRequest?> GetReservationRequestAsync(
        string caseRequestId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildEndpoint(_options.ReservationRequestEndpointTemplate, caseRequestId);
        return await httpClient.GetFromJsonAsync<CaseReservationRequest>(endpoint, cancellationToken);
    }

    public async Task<bool> ConfirmReservationAsync(
        string caseRequestId,
        string reservationNumber,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildEndpoint(_options.ConfirmReservationEndpointTemplate, caseRequestId);
        var payload = JsonContent.Create(new { reservationNumber });
        using var response = await httpClient.PostAsync(endpoint, payload, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static string BuildEndpoint(string template, string caseRequestId)
    {
        return template.Replace("{id}", Uri.EscapeDataString(caseRequestId), StringComparison.OrdinalIgnoreCase);
    }
}
