
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Service_bus.Configurations;
using Service_bus.LeaderElection;
using Service_bus.ServiceRegistry;

namespace Service_bus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClusterMetadataController
{
    private readonly ILeaderElectionClient _leaderElectionClient;
    private readonly IServiceBusRegistry _serviceBusRegistry;
    private readonly string _serviceId;

    public ClusterMetadataController(
        ILeaderElectionClient leaderElectionClient,
        IServiceBusRegistry serviceBusRegistry,
        IOptions<ConsulOptions> consulOptions)
    {
        _leaderElectionClient = leaderElectionClient;
        _serviceBusRegistry = serviceBusRegistry;
        _serviceId = consulOptions.Value.ServiceId;
    }

    [HttpGet("leader")]
    public bool IsLeader()
    {
        return _leaderElectionClient.IsLeader();
    }

    [HttpGet("instance/id")]
    public string GetInstanceId()
    {
        return _serviceId;
    }

    [HttpGet("instance")]
    public async Task<List<ServiceBusInstance>> GetInstancesAsync()
    {
        return await _serviceBusRegistry.GetServicesAsync();
    }
}
