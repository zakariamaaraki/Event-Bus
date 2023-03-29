namespace Service_bus.Models;

public abstract class AbstractEvent
{
    public Dictionary<string, string> Header { get; set; } = new();

    public abstract string Serialize();

    public abstract AbstractEvent Deserialize(string stringEvent);
}