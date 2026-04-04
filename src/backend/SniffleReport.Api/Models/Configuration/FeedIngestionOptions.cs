namespace SniffleReport.Api.Models.Configuration;

public sealed class FeedIngestionOptions
{
    public const string SectionName = "FeedIngestion";

    public Guid SystemUserId { get; set; } = new("00000000-0000-0000-0000-000000000001");

    public int HttpTimeoutSeconds { get; set; } = 30;

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryBaseDelaySeconds { get; set; } = 5;

    public string? SocrataAppToken { get; set; }

    public AlertThresholdOptions Thresholds { get; set; } = new();
}

public sealed class AlertThresholdOptions
{
    public int ModerateWowPercentage { get; set; } = 50;

    public int ModerateMinAbsoluteCount { get; set; } = 10;

    public int HighWowPercentage { get; set; } = 100;

    public int HighMinAbsoluteCount { get; set; } = 50;

    public int CriticalWowPercentage { get; set; } = 200;

    public int CriticalMinAbsoluteCount { get; set; } = 100;

    public int WeeksToEvaluate { get; set; } = 4;
}
