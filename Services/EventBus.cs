using Service_bus.Models;
using Service_bus.Exceptions;
using Service_bus.Configurations;
using Service_bus.Volumes;

using Microsoft.Extensions.Options;

namespace Service_bus.Services;

public class EventBus : IEventBus
{
    private readonly IEventDispatcher<Event> _eventDispatcher;
    private readonly ILogger<EventBus> _logger;
    private readonly IEventLogger<Event> _eventLogger;
    private readonly int _maxKeySize;
    private readonly int _maxBodySize;

    private const int MaxAckTimeout = 60 * 24; // 1 day
    private const int MaxQueueNameLength = 100;

    public EventBus(
        IEventDispatcher<Event> eventDispatcher,
        ILogger<EventBus> logger,
        IEventLogger<Event> eventLogger,
        IOptions<EventOptions> eventOptions)
    {
        _eventDispatcher = eventDispatcher;
        _logger = logger;
        _eventLogger = eventLogger;
        _maxKeySize = eventOptions.Value.MaxKeySize;
        _maxBodySize = eventOptions.Value.MaxBodySize;
    }

    public Task PushEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken, bool logEvent = true)
    {
        ValidateEvent(newEvent);
        _logger.LogInformation("Pushing new event into the queue {queueName}", queueName);
        return _eventDispatcher.PushAsync(queueName, newEvent, cancellationToken, logEvent);
    }

    private void ValidateEvent(Event incomingEvent)
    {
        if (incomingEvent.Body?.Length > _maxBodySize)
        {
            throw new InvalidEventException($"The body size exceed the maximum allowed size which is {_maxBodySize}");
        }
        if (incomingEvent.Key?.Length > _maxKeySize)
        {
            throw new InvalidEventException($"The key size exceed the maximum allowed size which is {_maxKeySize}");
        }
    }

    public async Task<(Event, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent)
    {
        (Event polledEvent, Guid eventId) = await _eventDispatcher.PollAsync(queueName, cancellationToken, logEvent);
        _logger.LogInformation("Event consumed under the id = {eventId}", eventId);
        return (polledEvent, eventId);
    }

    public Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent)
    {
        return _eventDispatcher.AckAsync(queueName, eventId, cancellationToken, logEvent);
    }

    public Task CreateQueueAsync(string queueName, int ackTimeout, CancellationToken cancellationToken, bool logEvent = true)
    {
        ValidateArguments(queueName, ackTimeout);

        var eventHandler = new EventHandler<Event>(_logger, _eventLogger, ackTimeout, queueName);
        _eventDispatcher.AddEventHandler(queueName, eventHandler);
        if (logEvent)
        {
            return _eventLogger.LogQueueCreationEventAsync(queueName, ackTimeout, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true)
    {
        if (string.IsNullOrEmpty(queueName))
        {
            throw new InvalidArgumentException("Queue name cannot be null or empty");
        }
        _eventDispatcher.RemoveEventHandler(queueName);

        if (logEvent)
        {
            return _eventLogger.LogQueueDeletionEventAsync(queueName, cancellationToken);
        }
        return Task.CompletedTask;
    }

    private static void ValidateArguments(string queueName, int ackTimeout)
    {
        if (ackTimeout < 1 || ackTimeout > MaxAckTimeout)
        {
            throw new InvalidArgumentException($"Ack timeout must be between 1 minute and {MaxAckTimeout} minutes");
        }
        if (string.IsNullOrEmpty(queueName))
        {
            throw new InvalidArgumentException("Queue can not be null or empty");
        }
        if (queueName.Length > MaxQueueNameLength)
        {
            throw new InvalidArgumentException($"Queue name must contains at most {MaxQueueNameLength} characters");
        }
    }

    public Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken)
    {
        return _eventDispatcher.GetListOfQueuesAsync(cancellationToken);
    }

    public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        return _eventDispatcher.GetQueueInfoAsync(queueName, cancellationToken);
    }

    public Task<Event> PeekAsync(string queueName, CancellationToken cancellationToken)
    {
        return _eventDispatcher.PeekAsync(queueName, cancellationToken);
    }

    public Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken)
    {
        return _eventDispatcher.TriggerTimeoutChecksAsync(cancellationToken);
    }
}