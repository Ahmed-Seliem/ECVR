namespace ECM.ReservationSystem.OpenIdSettings;

public class MockDataOptions
{
    public bool Enabled { get; set; }
    public bool SeedReservations { get; set; } = true;
    public bool ForceReseed { get; set; } = false;
}
