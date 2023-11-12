
using Microsoft.AspNetCore.Mvc.Filters;
using Service_bus.Exceptions;
using Service_bus.Headers;
using Service_bus.LeaderElection;

namespace Service_bus.Filters;

/// <summary>
/// Filter used to check if the current instance is a leader and allowed to perform some operations impacting queues state.
/// </summary>
public class LeaderElectionChecker : ActionFilterAttribute
{
    private readonly ILeaderElectionClient _leaderElectionClient;
    private readonly ILogger<LeaderElectionChecker> _logger;

    public LeaderElectionChecker(
        ILogger<LeaderElectionChecker> logger,
        ILeaderElectionClient leaderElectionClient)
    {
        _leaderElectionClient = leaderElectionClient;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        string dataSync = context.HttpContext.Request.Headers[WellKnownHeaders.DataSyncHeader];

        if (true.ToString().Equals(dataSync))
        {
            _logger.LogInformation("Start data synchronization received from the leader");
            return;
        }

        _logger.LogInformation("Start checking if this node is the leader");
        CheckLeaderElection();
        _logger.LogInformation("Leader election checks done!");
    }

    private void CheckLeaderElection()
    {
        if (!_leaderElectionClient.IsLeader())
        {
            throw new ServiceBusInvalidOperationException("Request rejected, i'm not the leader");
        }
    }
}
