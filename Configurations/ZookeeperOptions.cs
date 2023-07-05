
namespace Service_bus.Configurations;

public class ZookeeperOptions
{
    public string ConnectionString { get; set; }

    public int SessionTimeout { get; set; }

    public string NodeId { get; } = Guid.NewGuid().ToString();
}
