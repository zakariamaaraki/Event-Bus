
using Consul;
using Microsoft.Extensions.Options;
using Service_bus.Configurations;

namespace Service_bus.Consul;

/// <summary>
/// Consul hosted service.
/// </summary>
public class ConsulHostedService : IHostedService
{
    private CancellationTokenSource? _cts;
    private readonly IConsulClient _consulClient;
    private readonly IOptions<ConsulOptions> _consulConfig;
    private readonly ILogger<ConsulHostedService> _logger;
    private string? _registrationID;

    public ConsulHostedService(
        IConsulClient consulClient,
        IOptions<ConsulOptions> consulConfig,
        ILogger<ConsulHostedService> logger)
    {
        _logger = logger;
        _consulConfig = consulConfig;
        _consulClient = consulClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a linked token so we can trigger cancellation outside of this token's cancellation
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var uri = new Uri($"{_consulConfig.Value.Host}:{_consulConfig.Value.Port}");
        _registrationID = $"{_consulConfig.Value.ServiceId}";

        var registration = new AgentServiceRegistration()
        {
            ID = _registrationID,
            Name = _consulConfig.Value.ServiceName,
            Address = $"{uri.Scheme}://{uri.Host}",
            Port = _consulConfig.Value.Port,
            Tags = new[] { _consulConfig.Value.ServiceName },
            Check = new AgentServiceCheck()
            {
                HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}/api/health/status",
                Timeout = TimeSpan.FromSeconds(3),
                Interval = TimeSpan.FromSeconds(10)
            }
        };

        _logger.LogInformation("Registering in Consul");
        await _consulClient.Agent.ServiceDeregister(registration.ID, _cts.Token);
        await _consulClient.Agent.ServiceRegister(registration, _cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        _logger.LogInformation("Deregistering from Consul");
        try
        {
            await _consulClient.Agent.ServiceDeregister(_registrationID, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Deregisteration failed");
        }
    }
}
