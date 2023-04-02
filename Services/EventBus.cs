using Service_bus.Models;
using Service_bus.Exceptions;
using Service_bus.Configurations;
using Service_bus.Volumes;

using Microsoft.Extensions.Options;

namespace Service_bus.Services;

/// <summary>
/// The front door service to the queueing system. 
/// </summary>
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

    /// <summary>
    /// Push an Event to a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="newEvent">The Event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
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

    /// <summary>
    /// Poll an Event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task<(Event, Guid)></returns>
    public async Task<(Event, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent)
    {
        (Event polledEvent, Guid eventId) = await _eventDispatcher.PollAsync(queueName, cancellationToken, logEvent);
        _logger.LogInformation("Event consumed under the id = {eventId}", eventId);
        return (polledEvent, eventId);
    }

    /// <summary>
    /// Ack an Event already polled from the queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">A Guid referencing an event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    public Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent)
    {
        return _eventDispatcher.AckAsync(queueName, eventId, cancellationToken, logEvent);
    }

    /// <summary>
    /// Create a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="ackTimeout">The ack timeout for events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the queue?</param>
    /// <returns>A Task.</returns>
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

    /// <summary>
    /// Delete a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
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

    /// <summary>
    /// Get list of the queues.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of queue info.</returns>
    public Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken)
    {
        return _eventDispatcher.GetListOfQueuesAsync(cancellationToken);
    }

    /// <summary>
    /// Get a queue info.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo></returns>
    public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        return _eventDispatcher.GetQueueInfoAsync(queueName, cancellationToken);
    }

    /// <summary>
    /// Peek an event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<Event></returns>
    public Task<Event> PeekAsync(string queueName, CancellationToken cancellationToken)
    {
        return _eventDispatcher.PeekAsync(queueName, cancellationToken);
    }

    /// <summary>
    /// Trigger timeout checks.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    public Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken)
    {
        return _eventDispatcher.TriggerTimeoutChecksAsync(cancellationToken);
    }
}