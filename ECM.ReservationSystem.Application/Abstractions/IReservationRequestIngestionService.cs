using ECM.ReservationSystem.Application.Contracts.CaseWorkflow;

namespace ECM.ReservationSystem.Application.Abstractions;

public interface IReservationRequestIngestionService
{
    Task<CaseReservationRequest?> PullRequestFromCaseAsync(
        string caseRequestId,
        CancellationToken cancellationToken = default);
}
