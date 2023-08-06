
namespace Service_bus.Configurations;

/// <summary>
/// Consul configuration
/// </summary>
public class ConsulOptions
{
    /// <summary>
    /// Consul connection string
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// ServiceBus instance host, example: http://localhost.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// ServiceBus instance port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Service name: example: ServiceBus.
    /// </summary>
    public string ServiceName { get; set; }

    /// <summary>
    /// The instance id
    /// </summary>
    public string ServiceId { get; } = Guid.NewGuid().ToString();
}
