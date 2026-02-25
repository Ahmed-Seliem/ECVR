namespace ECM.ReservationSystem.Application.Contracts.CaseWorkflow;

public sealed class CaseReservationRequest
{
    public string RequestId { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string EmployeeNumber { get; init; } = string.Empty;
    public string Sector { get; init; } = string.Empty;
    public int CityId { get; init; }
    public DateOnly TravelDate { get; init; }
    public bool NeedsTransportation { get; init; }
    public DateTime ReceivedAtUtc { get; init; } = DateTime.UtcNow;
}
