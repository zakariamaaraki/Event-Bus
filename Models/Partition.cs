
namespace Service_bus.Models;

public class Partition
{
    public int UnAckedPollEvents { get; set; }

    public int NumberOfElements { get; set; }
}
