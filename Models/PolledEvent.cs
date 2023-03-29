namespace Service_bus.Models;

public class PolledEvent
{
    public Guid Id { get; set; }

    public Event? Event { get; set; }
}