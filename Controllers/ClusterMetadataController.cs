
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Service_bus.Configurations;
using Service_bus.LeaderElection;

namespace Service_bus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClusterMetadataController
{
    private readonly IZookeeperClient _zookeeperClient;
    private readonly string _nodeId;

    public ClusterMetadataController(IZookeeperClient zookeeperClient, IOptions<ZookeeperOptions> zookeeperOptions)
    {
        _zookeeperClient = zookeeperClient;
        _nodeId = zookeeperOptions.Value.NodeId;
    }

    [HttpGet("leader")]
    public Task<string> GetLeaderAsync()
    {
        return _zookeeperClient.GetLeaderAsync();
    }

    [HttpGet("nodeId")]
    public string GetNodeId()
    {
        return _nodeId;
    }
}
