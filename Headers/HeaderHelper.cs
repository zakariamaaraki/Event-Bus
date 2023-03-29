namespace Service_bus.Headers;

public static class HeaderHelper
{
    public static void AddDefaultValuesIfAbsentToTheHeader(Dictionary<string, string> header)
    {
        if (!header.ContainsKey(WellKnownHeaders.StartProcessingTimestampHeader))
        {
            header[WellKnownHeaders.StartProcessingTimestampHeader] = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
        }

        header[WellKnownHeaders.LastProcessingTimestampHeader] = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();

        if (!header.ContainsKey(WellKnownHeaders.MaxNumberOfAckTimeoutsHeader))
        {
            header[WellKnownHeaders.MaxNumberOfAckTimeoutsHeader] = "-1"; // No limit
        }

        header[WellKnownHeaders.CurrentNumberOfAckTimeoutsHeader] = "0";
    }

    public static void UpdateLastProcessingTimestamp(Dictionary<string, string> header)
    {
        header[WellKnownHeaders.LastProcessingTimestampHeader] = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
    }

    public static int GetMaxAckTimeout(Dictionary<string, string> header)
    {
        if (header.TryGetValue(WellKnownHeaders.MaxNumberOfAckTimeoutsHeader, out string? value))
        {
            return Int32.Parse(value);
        }
        return -1;
    }

    public static int IncrementCurrentNumberOfAckTimeouts(Dictionary<string, string> header)
    {
        if (header.TryGetValue(WellKnownHeaders.CurrentNumberOfAckTimeoutsHeader, out string? value))
        {
            var currentNumberOfAckTimeouts = Int32.Parse(value);
            if (currentNumberOfAckTimeouts < Int32.MaxValue)
            {
                currentNumberOfAckTimeouts++;
                header[WellKnownHeaders.CurrentNumberOfAckTimeoutsHeader] = (currentNumberOfAckTimeouts + 1).ToString();
            }
            return currentNumberOfAckTimeouts;
        }
        return -1;
    }

    public static bool ShouldBeRequeued(Dictionary<string, string> header)
    {
        int maxAckTimeouts = GetMaxAckTimeout(header);

        if (maxAckTimeouts == -1)
        {
            return true;
        }

        if (header.TryGetValue(WellKnownHeaders.CurrentNumberOfAckTimeoutsHeader, out string? currentValue))
        {
            var currentNumberOfAckTimeouts = Int32.Parse(currentValue);
            return currentNumberOfAckTimeouts < maxAckTimeouts;
        }

        return true;
    }
}