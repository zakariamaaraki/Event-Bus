using Consul;
using Microsoft.Extensions.Options;
using Service_bus.Configurations;

namespace Service_bus.ServiceRegistry;

/// <summary>
/// ServiceBus registry implementation using Consul.
/// </summary>
public class ServiceBusRegistry : IServiceBusRegistry
{
    private readonly IConsulClient _consulClient;
    private readonly ConsulOptions _consulConfig;

    public ServiceBusRegistry(IConsulClient consulClient, IOptions<ConsulOptions> consulConfig)
    {
        _consulClient = consulClient;
        _consulConfig = consulConfig.Value;
    }

    /// <summary>
    /// Get a list of services registered with some defined tags.
    /// </summary>
    /// <returns>A list of services</returns>
    public async Task<List<ServiceBusInstance>> GetServicesAsync()
    {
        QueryResult<Dictionary<string, AgentService>> services = await _consulClient.Agent.Services();

        return services.Response
                            .Where(service => service.Value.Tags.Any(t => t == _consulConfig.ServiceName))
                            .Select(service => new ServiceBusInstance()
                            {
                                Host = service.Value.Address,
                                Id = service.Value.ID,
                                Port = service.Value.Port,
                                Tags = service.Value.Tags
                            })
                            .ToList();
    }
}