using ECM.ReservationSystem.Models.DTOs;

namespace ECM.ReservationSystem.Services.Interfaces;

public interface IReservationSubmissionAttemptService
{
    Task LogAsync(
        ReservationRequestDto request,
        string outcome,
        int? reservationId = null,
        string? failureReason = null,
        string? requestPayload = null,
        string? responsePayload = null);
}
