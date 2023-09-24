
using System.Net;
using Microsoft.Extensions.Options;
using Service_bus.Configurations;
using Service_bus.Models;
using Service_bus.ServiceRegistry;

namespace Service_bus.DataReplication;

public class LeaderToFollowersDataReplication : ILeaderToFollowersDataReplication
{

    private readonly IServiceBusRegistry _serviceBusRegistry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LeaderToFollowersDataReplication> _logger;
    private readonly string _serviceId;

    public LeaderToFollowersDataReplication(
        IServiceBusRegistry serviceBusRegistry,
        IHttpClientFactory httpClientFactory,
        IOptions<ConsulOptions> consulOptions,
        ILogger<LeaderToFollowersDataReplication> logger)
    {
        _serviceBusRegistry = serviceBusRegistry;
        _httpClientFactory = httpClientFactory;
        _serviceId = consulOptions.Value.ServiceId;
        _logger = logger;
    }

    public async Task<bool> PushNewEventAsync(string queueName, Event newEvent, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}/queue/event?queueName={queueName}");
        request.Content = JsonContent.Create(newEvent);

        return await Replicate(request, followerId);
    }

    public async Task<bool> PushNewEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await PushNewEventAsync(queueName, newEvent, instanceId, cancellationToken));

    }

    public async Task<bool> CreateQueueAsync(string queueName, int numberOfPartitions, int maxAckTimeout, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, $"{baseUri}/queue?queueName={queueName}&numberOfPartitions={numberOfPartitions}&maxAckTimeout={maxAckTimeout}");
        return await Replicate(request, followerId);
    }

    public async Task<bool> CreateQueueAsync(string queueName, int numberOfPartitions, int maxAckTimeout, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await CreateQueueAsync(queueName, numberOfPartitions, maxAckTimeout, instanceId, cancellationToken));
    }


    private async Task<bool> ReplicateToAllInstances(Func<string, Task<bool>> operation)
    {
        // Get followers metadata
        List<ServiceBusInstance> serviceInstances = await _serviceBusRegistry.GetServicesAsync();
        foreach (ServiceBusInstance serviceBusInstance in serviceInstances)
        {
            if (serviceBusInstance.Id.Equals(_serviceId))
            {
                continue;
            }

            bool response = await operation(serviceBusInstance.Id);
            if (response == false)
            {
                // TODO: add in-sync replicas
                return false;
            }
        }
        return true;
    }

    private async Task<bool> Replicate(HttpRequestMessage request, string followerId)
    {
        // Create the client
        using HttpClient client = _httpClientFactory.CreateClient();

        try
        {
            // Make HTTP Post request
            HttpResponseMessage httpResponseMessage = await client.SendAsync(request);

            if (!ValidateResponse(httpResponseMessage))
            {
                _logger.LogWarning(
                    "Error while replicating data to the follower Id = {FollowerId} Status Code = {Error}",
                    followerId,
                    httpResponseMessage.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while replicating data to the follower Id = {FollowerId} Error = {Error}", followerId, ex);
            return false;
        }

        return true;
    }

    private static bool ValidateResponse(HttpResponseMessage httpResponseMessage)
    {
        return httpResponseMessage.StatusCode.Equals(HttpStatusCode.Accepted);
    }
}
