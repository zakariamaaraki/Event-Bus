namespace Service_bus.Models;

public class QueueInfo
{
    public string QueueName { get; set; }

    public int AckTimeout { get; set; }

    public int NumberOfPartitions { get; set; }

    public Dictionary<string, Partition> Partitions { get; set; }
}