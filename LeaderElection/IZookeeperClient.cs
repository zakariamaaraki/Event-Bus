
namespace Service_bus.LeaderElection;

public interface IZookeeperClient
{
    Task<bool> CheckLeaderAsync();
    Task<string> GetLeaderAsync();
}