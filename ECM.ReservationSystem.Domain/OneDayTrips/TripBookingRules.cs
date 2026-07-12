namespace ECM.ReservationSystem.Domain.OneDayTrips;

public static class TripBookingRules
{
    public const int MinAdultsPerBooking = 1;
    public const int MaxGuestsPerBooking = 5;

    // Payment window before an unpaid booking is auto-cancelled and its tickets released.
    public const int PaymentHoldWorkingDays = 1;

    // Advance the given number of working days, skipping Friday & Saturday (Egyptian weekend).
    public static DateTime ComputePaymentDeadline(DateTime from, int workingDays = PaymentHoldWorkingDays)
    {
        var result = from;
        var added = 0;

        while (added < workingDays)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek != DayOfWeek.Friday && result.DayOfWeek != DayOfWeek.Saturday)
            {
                added++;
            }
        }

        return result;
    }
}
