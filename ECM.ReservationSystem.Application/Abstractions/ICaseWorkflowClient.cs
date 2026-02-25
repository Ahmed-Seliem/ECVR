using ECM.ReservationSystem.Application.Contracts.CaseWorkflow;

namespace ECM.ReservationSystem.Application.Abstractions;

public interface ICaseWorkflowClient
{
    Task<CaseReservationRequest?> GetReservationRequestAsync(
        string caseRequestId,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmReservationAsync(
        string caseRequestId,
        string reservationNumber,
        CancellationToken cancellationToken = default);
}
