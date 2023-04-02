namespace Service_bus.Models;

public class PolledEvent
{
    /// <summary>
    /// The id of the event, used to map an ack received by the called, to the event stored in the cache.
    /// </summary>
    /// <value>A Guid</value>
    public Guid Id { get; set; }

    /// <summary>
    /// The event.
    /// </summary>
    /// <value>The Event.</value>
    public Event? Event { get; set; }
}