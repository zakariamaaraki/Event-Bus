
using Microsoft.AspNetCore.Mvc.Filters;
using Service_bus.LeaderElection;

namespace Service_bus.Middlewares;

public class LeaderElectionChecker : ActionFilterAttribute
{
    private readonly IZookeeperClient _zookeeperClient;
    private readonly ILogger<LeaderElectionChecker> _logger;

    public LeaderElectionChecker(ILogger<LeaderElectionChecker> logger, IZookeeperClient zookeeperClient)
    {
        _zookeeperClient = zookeeperClient;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _logger.LogInformation("Start checking if this node is the leader");
        CheckLeaderElection().Wait();
        _logger.LogInformation("Leader election checks done!");
    }

    private async Task CheckLeaderElection()
    {
        bool checkLeaderElection = await _zookeeperClient.CheckLeaderAsync();

        if (!checkLeaderElection)
        {
            throw new InvalidOperationException("Request rejected, i'm not the leader");
        }
    }
}
