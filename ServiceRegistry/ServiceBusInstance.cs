
namespace Service_bus.ServiceRegistry;

/// <summary>
/// ServiceBus instance
/// </summary>
public class ServiceBusInstance
{
    /// <summary>
    /// The id of the instance.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The host name of the instance, example: http://localhost, or http://node1
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// The port of the instance.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Tags used to register the instance.
    /// </summary>
    public string[] Tags { get; set; }
}