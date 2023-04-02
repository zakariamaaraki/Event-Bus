namespace Service_bus.Models;

public class QueueInfo
{
    public string QueueName { get; set; }

    public int AckTimeout { get; set; }

    public int UnAckedPollEvents { get; set; }

    public int NumberOfElements { get; set; }
}