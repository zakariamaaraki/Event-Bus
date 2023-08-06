
using Consul;
using Microsoft.Extensions.Options;
using SBTech.Consul.LeaderElection;
using Service_bus.Configurations;

namespace Service_bus.LeaderElection;

/// <summary>
/// Leader election implementation using Consul
/// </summary>
public class ConsulLeaderElection : ILeaderElectionClient
{
    private LeaderElectionMonitor? electionMonitor;
    private readonly IConsulClient _consulClient;

    public ConsulLeaderElection(IOptions<ConsulOptions> consulOptions, IConsulClient consulClient)
    {
        _consulClient = consulClient;
        StartLeaderElection(consulOptions.Value);
    }

    private void StartLeaderElection(ConsulOptions consulOptions)
    {
        var config = ElectionMonitorConfig.Default(serviceName: consulOptions.ServiceName, client: (ConsulClient)_consulClient);

        electionMonitor = new(config);
        electionMonitor.LeaderChanged += (s, e) =>
            {
                if (e.IsLeader)
                    Console.WriteLine($"[Master] at {DateTime.Now.ToString("hh:mm:ss")}");
                else
                    Console.WriteLine($"[Slave] at {DateTime.Now.ToString("hh:mm:ss")}");
            };

        var joinedCluster = electionMonitor.Start().Wait(timeout: TimeSpan.FromSeconds(30));
        if (joinedCluster)
        {
            if (electionMonitor.IsLeader)
                Console.WriteLine($"Joined cluster as [Master] at {DateTime.Now.ToString("hh:mm:ss")}");
            else
                Console.WriteLine($"Joined cluster as [Slave] at {DateTime.Now.ToString("hh:mm:ss")}");
        }
        else
        {
            Console.WriteLine($"TestNode failed to join cluster at {DateTime.Now.ToString("hh:mm:ss")}");
        }

        electionMonitor.Start();
    }

    public bool IsLeader()
    {
        return electionMonitor?.IsLeader ?? false;
    }
}
