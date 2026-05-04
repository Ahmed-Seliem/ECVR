using System.Text.Json;
using ECM.ReservationSystem.Data;
using ECM.ReservationSystem.Domain.Entities;
using ECM.ReservationSystem.Models.DTOs;
using ECM.ReservationSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.ReservationSystem.Services.Implementations;

public class ReservationSubmissionAttemptService : IReservationSubmissionAttemptService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IServiceScopeFactory _scopeFactory;

    public ReservationSubmissionAttemptService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task LogAsync(
        ReservationRequestDto request,
        string outcome,
        int? reservationId = null,
        string? failureReason = null,
        string? requestPayload = null,
        string? responsePayload = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var attempt = new ReservationSubmissionAttempt
        {
            Outcome = outcome,
            ReservationId = reservationId,
            DocumentId = request.DocumentId,
            WorkflowId = request.WorkflowId,
            EmployeeNumber = request.EmployeeNumber,
            EmployeeName = request.EmployeeName,
            Sector = request.Sector,
            PhoneNumber = request.PhoneNumber,
            UnitId = request.UnitId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            NumberOfGuests = request.NumberOfGuests,
            IsTransportationRequired = request.IsTransportationRequired,
            PaymentReceiptNumber = request.PaymentReceiptNumber,
            InsuranceReceiptNumber = request.InsuranceReceiptNumber,
            Notes = request.Notes,
            CaseSystemId = request.CaseSystemId,
            FailureReason = failureReason ?? string.Empty,
            RequestPayload = requestPayload ?? JsonSerializer.Serialize(request, JsonOptions),
            ResponsePayload = responsePayload ?? string.Empty
        };

        context.ReservationSubmissionAttempts.Add(attempt);
        await context.SaveChangesAsync();
    }
}
