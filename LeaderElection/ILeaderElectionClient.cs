
namespace Service_bus.LeaderElection;

/// <summary>
/// Leader election
/// </summary>
public interface ILeaderElectionClient
{
    /// <summary>
    /// Check if the current instance is a leader.
    /// </summary>
    /// <returns>True or False, wether the instance is a leader or not.</returns>
    bool IsLeader();
}