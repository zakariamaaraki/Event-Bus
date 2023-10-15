
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

        using HttpRequestMessage request = new(HttpMethod.Post, $"{baseUri}/queue/event?queueName={queueName}")
        {
            Content = JsonContent.Create(newEvent)
        };

        return await Replicate(request, followerId);
    }

    public async Task<bool> PushNewEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await PushNewEventAsync(queueName, newEvent, instanceId, cancellationToken));

    }

    public async Task<bool> PollEventAsync(string queueName, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        using HttpRequestMessage request = new(HttpMethod.Get, $"{baseUri}/queue/event?queueName={queueName}");

        // TODO: check the result returned by poll operation, and validate data consistency.

        return await Replicate(request, followerId);
    }

    public async Task<bool> PollEventAsync(string queueName, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await PollEventAsync(queueName, instanceId, cancellationToken));
    }

    public async Task<bool> DeleteQueueAsync(string queueName, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        using HttpRequestMessage request = new(HttpMethod.Delete, $"{baseUri}/queue?queueName={queueName}");

        // TODO: check the result returned by poll operation, and validate data consistency.

        return await Replicate(request, followerId);
    }

    public async Task<bool> DeleteQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await DeleteQueueAsync(queueName, instanceId, cancellationToken));
    }

    public async Task<bool> ClearQueueAsync(string queueName, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        using HttpRequestMessage request = new(HttpMethod.Delete, $"{baseUri}/queue/events?queueName={queueName}");

        // TODO: check the result returned by poll operation, and validate data consistency.

        return await Replicate(request, followerId);
    }

    public async Task<bool> ClearQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await ClearQueueAsync(queueName, instanceId, cancellationToken));
    }

    public async Task<bool> ScaleNumberOfPartitionsAsync(string queueName, int numberOfPartitions, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        using HttpRequestMessage request = new(HttpMethod.Post, $"{baseUri}/queue/scale?queueName={queueName}")
        {
            Content = JsonContent.Create(numberOfPartitions)
        };

        return await Replicate(request, followerId);
    }

    public async Task<bool> ScaleNumberOfPartitionsAsync(string queueName, int numberOfPartitions, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await ScaleNumberOfPartitionsAsync(queueName, numberOfPartitions, instanceId, cancellationToken));

    }

    public async Task<bool> AckEventAsync(string queueName, Guid eventId, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        using HttpRequestMessage request = new(HttpMethod.Post, $"{baseUri}/queue/ack?queueName={queueName}")
        {
            Content = JsonContent.Create(eventId)
        };

        return await Replicate(request, followerId);
    }

    public async Task<bool> AckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await AckEventAsync(queueName, eventId, instanceId, cancellationToken));

    }

    public async Task<bool> AckEventAndMoveItToDLQAsync(string queueName, Guid eventId, string followerId, CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        using HttpRequestMessage request = new(HttpMethod.Post, $"{baseUri}/queue/deadletter?queueName={queueName}")
        {
            Content = JsonContent.Create(eventId)
        };

        return await Replicate(request, followerId);
    }

    public async Task<bool> AckEventAndMoveItToDLQAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        return await ReplicateToAllInstances(
            async (instanceId) => await AckEventAndMoveItToDLQAsync(queueName, eventId, instanceId, cancellationToken));

    }

    public async Task<bool> CreateQueueAsync(
        string queueName,
        int numberOfPartitions,
        int maxAckTimeout,
        string followerId,
        CancellationToken cancellationToken)
    {
        // Get follower metadata
        ServiceBusInstance serviceInstance = await _serviceBusRegistry.GetServiceByIdAsync(followerId);

        string baseUri = $"{serviceInstance.Host}:{serviceInstance.Port}/api/ServiceBus";

        using HttpRequestMessage request =
            new(HttpMethod.Post, $"{baseUri}/queue?queueName={queueName}&numberOfPartitions={numberOfPartitions}&maxAckTimeout={maxAckTimeout}");

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
            using HttpResponseMessage httpResponseMessage = await client.SendAsync(request);

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
