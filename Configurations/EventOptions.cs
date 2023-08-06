namespace Service_bus.Configurations;

/// <summary>
/// Event configuration.
/// </summary>
public class EventOptions
{
    /// <summary>
    /// Maximum Key size of an event.
    /// </summary>
    public int MaxKeySize { get; set; }

    /// <summary>
    /// Maximum Body size of an event.
    /// </summary>
    public int MaxBodySize { get; set; }
}