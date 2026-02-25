namespace ECM.ReservationSystem.Infrastructure.Integrations.CaseWorkflow;

public sealed class CaseWorkflowOptions
{
    public const string SectionName = "CaseWorkflow";

    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ReservationRequestEndpointTemplate { get; init; } = "/api/requests/{id}";
    public string ConfirmReservationEndpointTemplate { get; init; } = "/api/requests/{id}/confirm";
}
