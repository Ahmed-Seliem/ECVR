using ECM.ReservationSystem.Application.Abstractions;
using ECM.ReservationSystem.Application.Contracts.CaseWorkflow;

namespace ECM.ReservationSystem.Application.Services;

public sealed class ReservationRequestIngestionService(ICaseWorkflowClient caseWorkflowClient)
    : IReservationRequestIngestionService
{
    public Task<CaseReservationRequest?> PullRequestFromCaseAsync(
        string caseRequestId,
        CancellationToken cancellationToken = default)
    {
        return caseWorkflowClient.GetReservationRequestAsync(caseRequestId, cancellationToken);
    }
}
