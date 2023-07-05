
using Microsoft.Extensions.Options;
using org.apache.zookeeper;
using org.apache.zookeeper.recipes.leader;
using Service_bus.Configurations;
using static org.apache.zookeeper.Watcher.Event;
using static org.apache.zookeeper.ZooDefs;

namespace Service_bus.LeaderElection;

public sealed class ZookeeperClient : IZookeeperClient, IDisposable
{
    private const string RootNode = "/service-bus-leader-election";

    private readonly string _connectionString;
    private readonly int _sessionTimeout;

    private readonly string _nodeId;

    private ZooKeeper? _zookeeper;
    private LeaderElectionSupport? _leaderElection;

    public ZookeeperClient(IOptions<ZookeeperOptions> zookeeperOptions)
    {
        _connectionString = zookeeperOptions.Value.ConnectionString;
        _sessionTimeout = zookeeperOptions.Value.SessionTimeout;
        _nodeId = zookeeperOptions.Value.NodeId;
    }

    public async Task<bool> CheckLeaderAsync()
    {
        if (_leaderElection is null)
        {
            var watcher = new ConnectionWatcher();
            _zookeeper = new ZooKeeper(_connectionString, _sessionTimeout, watcher);

            await watcher.WaitForConnectionAsync();

            if (await _zookeeper.existsAsync(RootNode) is null)
            {
                await _zookeeper.createAsync(RootNode, Array.Empty<byte>(), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
            }

            _leaderElection = new LeaderElectionSupport(_zookeeper, RootNode, _nodeId);
            await _leaderElection.start();
        }
        var leaderHostName = await GetLeaderAsync();
        return leaderHostName == _nodeId;
    }

    public Task<string> GetLeaderAsync()
    {
        if (_leaderElection is null)
        {
            throw InvalidOperationException("This operation cannot be done before the leader election!");
        }
        return _leaderElection.getLeaderHostName();
    }

    private Exception InvalidOperationException(string v)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (_leaderElection is not null)
        {
            _leaderElection.stop().Wait();

        }
        if (_zookeeper is not null)
        {
            _zookeeper.closeAsync().Wait();
        }
    }

    private sealed class ConnectionWatcher : Watcher
    {
        private readonly TaskCompletionSource _tcs = new();

        public Task WaitForConnectionAsync() => _tcs.Task;

        public override Task process(WatchedEvent @event)
        {
            if (@event.getState() is KeeperState.SyncConnected)
            {
                _tcs.TrySetResult();
            }
            return Task.CompletedTask;
        }
    }
}