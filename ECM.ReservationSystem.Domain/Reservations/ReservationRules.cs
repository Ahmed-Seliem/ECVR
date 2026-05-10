namespace ECM.ReservationSystem.Domain.Reservations;

public static class ReservationRules
{
    public static readonly TimeSpan PreSubmitHoldDuration = TimeSpan.FromMinutes(3);
    public const int SubmittedHoldBusinessDays = 3;
}
