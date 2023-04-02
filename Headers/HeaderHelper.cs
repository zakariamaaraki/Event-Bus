namespace Service_bus.Headers;

/// <summary>
/// Helper class used to handle and transform header key, value pair.
/// </summary>
public static class HeaderHelper
{
    /// <summary>
    /// This method adds default values to the header of an event if certain key are not found
    /// </summary>
    /// <param name="header">A dictionary representing the key, value pairs of the header</param>
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

    /// <summary>
    /// This method updates the last processing timestamp in the header of the event
    /// </summary>
    /// <param name="header">A dictionary representing the key, value pairs of the header</param>
    public static void UpdateLastProcessingTimestamp(Dictionary<string, string> header)
    {
        header[WellKnownHeaders.LastProcessingTimestampHeader] = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
    }

    /// <summary>
    /// This method retrieves the maximum ack timeout from the header
    /// </summary>
    /// <param name="header">A dictionary representing the key, value pairs of the header</param>
    /// <returns>Maximum ack timeout</returns>
    public static int GetMaxAckTimeout(Dictionary<string, string> header)
    {
        if (header.TryGetValue(WellKnownHeaders.MaxNumberOfAckTimeoutsHeader, out string? value))
        {
            return Int32.Parse(value);
        }
        return -1;
    }

    /// <summary>
    /// This method increments and returns the current number of acks timeout of the event
    /// </summary>
    /// <param name="header">A dictionary representing the key, value pairs of the header</param>
    /// <returns>The number of acks timeout of the event</returns>
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

    /// <summary>
    /// This method return whether the event should be requeued or not after an ack timeout.
    /// It compares the maxAckTimeout value in the header with the currentAckTimeout.
    /// </summary>
    /// <param name="header">A dictionary representing the key, value pairs of the header</param>
    /// <returns>A boolean representing whether the event should be requeued or not after an ack timeout.</returns>
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