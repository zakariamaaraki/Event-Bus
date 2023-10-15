using Microsoft.AspNetCore.Mvc;
using Service_bus.Models;
using Service_bus.Services;
using Service_bus.Headers;
using Service_bus.Filters;

namespace Service_bus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceBusController
{
    private readonly IEventBus _eventBus;

    public ServiceBusController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost("queue")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task CreateQueue(string queueName, int numberOfPartitions, int maxAckTimeout, CancellationToken cancellationToken)
    {
        await _eventBus.CreateQueueAsync(queueName, maxAckTimeout, cancellationToken, numberOfPartitions: numberOfPartitions);
    }

    [HttpPost("queue/scale")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task ScaleNumberOfPartitions(string queueName, int newNumberOfPartitions, CancellationToken cancellationToken)
    {
        await _eventBus.ScaleNumberOfPartitions(queueName, newNumberOfPartitions, cancellationToken);
    }

    [HttpPost("queue/event")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task PushNewEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken)
    {
        HeaderHelper.AddDefaultValuesIfAbsentToTheHeader(newEvent.Header);
        await _eventBus.PushEventAsync(queueName, newEvent, cancellationToken);
    }

    [HttpPost("queue/ack")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task AckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        await _eventBus.AckAsync(queueName, eventId, cancellationToken);
    }

    [HttpPost("queue/deadletter")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task AckAndMoveToDeadLetterQueueAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        await _eventBus.AckAndMoveToDeadLetterQueue(queueName, eventId, cancellationToken);
    }

    [HttpGet("queue/info/{queueName}")]
    public async Task<QueueInfo> GetQueueSizeInByteAsync(string queueName, CancellationToken cancellationToken)
    {
        return await _eventBus.GetQueueInfoAsync(queueName, cancellationToken);
    }

    [HttpGet("queues")]
    public async Task<QueuesInfo> GetQueues(CancellationToken cancellationToken)
    {
        return new QueuesInfo
        {
            Queues = await _eventBus.GetListOfQueuesAsync(cancellationToken)
        };
    }

    [HttpGet("queue/event/{queueName}")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task<PolledEvent> PollEventAsync(string queueName, CancellationToken cancellationToken)
    {
        (Event polledEvent, Guid id) = await _eventBus.PollAsync(queueName, cancellationToken);

        return new PolledEvent
        {
            Event = polledEvent,
            Id = id // This id is used to Ack the event
        };
    }

    [HttpGet("queue/event/peek/{queueName}")]
    public async Task<PolledEvent> PeekEventAsync(string queueName, CancellationToken cancellationToken)
    {
        Event peekedEvent = await _eventBus.PeekAsync(queueName, cancellationToken);

        return new PolledEvent
        {
            Event = peekedEvent,
            Id = Guid.Empty
        };
    }

    [HttpDelete("queue/{queueName}")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        await _eventBus.DeleteQueueAsync(queueName, cancellationToken);
    }

    [HttpDelete("queue/events/{queueName}")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public async Task ClearQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        await _eventBus.Clear(queueName, cancellationToken);
    }
}