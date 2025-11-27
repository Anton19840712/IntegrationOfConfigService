namespace Application.Settings
{
    public class RefreshTokenCleanupSettings
    {
        public bool Enabled { get; set; } = true;
        public int CleanupIntervalHours { get; set; } = 24;
        public int DaysToKeep { get; set; } = 30;
        public int BatchSize { get; set; } = 1000;
        public int MaxRetentionDays { get; set; } = 90;
    }
}