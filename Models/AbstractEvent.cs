namespace Service_bus.Models;

public abstract class AbstractEvent
{
    public Dictionary<string, string> Header { get; set; } = new();

    /// <summary>
    /// This method serializes the event into a string. 
    /// </summary>
    /// <returns>A string representation of the event.</returns>
    public abstract string Serialize();

    /// <summary>
    /// This method deserializes a string repreentation of the event into it's original representation
    /// which is a subclass of AbstractEvent.
    /// </summary>
    /// <param name="stringEvent">A string representation of the event.</param>
    /// <returns>Returns an AbstractEvent.</returns>
    public abstract AbstractEvent Deserialize(string stringEvent);
}