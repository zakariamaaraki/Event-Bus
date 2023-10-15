namespace Service_bus.Headers;

/// <summary>
/// Header constants
/// </summary>
public static class WellKnownHeaders
{
    public const string StartProcessingTimestampHeader = "x-start-processing-timestamp";
    public const string LastProcessingTimestampHeader = "x-last-processing-timestamp";
    public const string MaxNumberOfAckTimeoutsHeader = "x-max-number-of-ack-timeouts";
    public const string CurrentNumberOfAckTimeoutsHeader = "x-current-number-of-ack-timeouts";
    public const string SendToDeadLetterQueueAfterAckTimeout = "x-send-to-dlq-after-ack-timeout";
    public const string DataSyncHeader = "x-data-sync";
}