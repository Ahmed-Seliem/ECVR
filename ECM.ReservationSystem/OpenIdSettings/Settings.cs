namespace ECM.ReservationSystem.OpenIdSettings
{
    public class Settings
    {
        public Identity Identity { get; set; }
    }

    public class Identity
    {
        public string ServerURL { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
