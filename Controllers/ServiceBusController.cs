using Microsoft.AspNetCore.Mvc;
using Service_bus.Models;
using Service_bus.Services;
using Service_bus.Headers;
using Service_bus.Middlewares;

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
    public Task CreateQueue(string queueName, int numberOfPartitions, int maxAckTimeout, CancellationToken cancellationToken)
    {
        return _eventBus.CreateQueueAsync(queueName, maxAckTimeout, cancellationToken, numberOfPartitions: numberOfPartitions);
    }

    [HttpPost("queue/scale")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public Task ScaleNumberOfPartitions(string queueName, int newNumberOfPartitions, CancellationToken cancellationToken)
    {
        return _eventBus.ScaleNumberOfPartitions(queueName, newNumberOfPartitions, cancellationToken);
    }

    [HttpPost("queue/event")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public Task PushNewEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken)
    {
        HeaderHelper.AddDefaultValuesIfAbsentToTheHeader(newEvent.Header);
        return _eventBus.PushEventAsync(queueName, newEvent, cancellationToken);
    }

    [HttpPost("queue/ack")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public Task AckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        return _eventBus.AckAsync(queueName, eventId, cancellationToken);
    }

    [HttpPost("queue/deadletter")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public Task AckAndMoveToDeadLetterQueueAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        return _eventBus.AckAndMoveToDeadLetterQueue(queueName, eventId, cancellationToken);
    }

    [HttpGet("queue/info/{queueName}")]
    public Task<QueueInfo> GetQueueSizeInByteAsync(string queueName, CancellationToken cancellationToken)
    {
        return _eventBus.GetQueueInfoAsync(queueName, cancellationToken);
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
    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        return _eventBus.DeleteQueueAsync(queueName, cancellationToken);
    }

    [HttpDelete("queue/events/{queueName}")]
    [ServiceFilter(typeof(LeaderElectionChecker))]
    public Task ClearQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        return _eventBus.Clear(queueName, cancellationToken);
    }
}