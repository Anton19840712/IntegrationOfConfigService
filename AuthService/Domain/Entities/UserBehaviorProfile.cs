namespace Domain.Entities
{
    public class UserBehaviorProfile
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        // Храним до 20 последних известных IP
        public List<string> KnownIpAddresses { get; set; } = new();
        // Часы активности (удобно анализировать для аномалий)
        public List<int> TypicalActiveHoursUtc { get; set; } = new();
        // UserAgents
        public List<string> KnownUserAgents { get; set; } = new();

        public DateTime LastUpdatedAt { get; set; }
    }
}
