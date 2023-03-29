namespace Service_bus.Headers;

public static class WellKnownHeaders
{
    public const string StartProcessingTimestampHeader = "start-processing-timestamp";
    public const string LastProcessingTimestampHeader = "last-processing-timestamp";
    public const string MaxNumberOfAckTimeoutsHeader = "max-number-of-ack-timeouts";
    public const string CurrentNumberOfAckTimeoutsHeader = "current-number-of-ack-timeouts";
}