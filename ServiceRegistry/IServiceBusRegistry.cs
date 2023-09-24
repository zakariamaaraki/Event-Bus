
namespace Service_bus.ServiceRegistry;

/// <summary>
/// ServiceBus service registry
/// </summary>
public interface IServiceBusRegistry
{
    /// <summary>
    /// Get a list of services registered with some defined tags.
    /// </summary>
    /// <returns>A list of services.</returns>
    Task<List<ServiceBusInstance>> GetServicesAsync();

    /// <summary>
    /// Get a registered service by Id.
    /// </summary>
    /// <param name="serviceId">The service Id.</param>
    /// <returns>The service.</returns>
    Task<ServiceBusInstance> GetServiceByIdAsync(string serviceId);
}
