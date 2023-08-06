
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Service_bus.Configurations;
using Service_bus.LeaderElection;

namespace Service_bus.Filters;

/// <summary>
/// Filter used to check if the current instance is a leader and allowed to perform some operations impacting queues state.
/// </summary>
public class LeaderElectionChecker : ActionFilterAttribute
{
    private readonly ILeaderElectionClient _leaderElectionClient;
    private readonly ILogger<LeaderElectionChecker> _logger;
    private readonly string _serviceId;

    public LeaderElectionChecker(
        ILogger<LeaderElectionChecker> logger,
        ILeaderElectionClient leaderElectionClient,
        IOptions<ConsulOptions> consulOptions)
    {
        _leaderElectionClient = leaderElectionClient;
        _logger = logger;
        _serviceId = consulOptions.Value.ServiceId;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _logger.LogInformation("Start checking if this node is the leader");
        CheckLeaderElection();
        _logger.LogInformation("Leader election checks done!");
    }

    private void CheckLeaderElection()
    {
        if (!_leaderElectionClient.IsLeader())
        {
            throw new InvalidOperationException("Request rejected, i'm not the leader");
        }
    }
}
