using Service_bus.Models;
using Service_bus.Exceptions;

namespace Service_bus.Services;

/// <summary>
/// Orchestrates events to the right event handler. 
/// </summary>
/// <typeparam name="T">The tipe of Event (subclass of AbstractEvent).</typeparam>
public class EventDispatcher<T> : IEventDispatcher<T> where T : AbstractEvent
{
    private readonly Dictionary<string, IEventHandler<T>> _eventHandlers = new();
    private readonly ILogger<EventDispatcher<T>> _logger;

    public EventDispatcher(ILogger<EventDispatcher<T>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add an event handler to the internal Dictionary.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventHandler">The event handler.</param>
    public void AddEventHandler(string queueName, IEventHandler<T> eventHandler)
    {
        if (_eventHandlers.ContainsKey(queueName))
        {
            throw new QueueAlreadyExistsException($"Queue {queueName} already exists, please provide another name");
        }
        _eventHandlers[queueName] = eventHandler;
        _logger.LogInformation("New event handler registered with queue name = {queueName}", queueName);
    }

    /// <summary>
    /// Remove an event handler from the internal Dictionary.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    public void RemoveEventHandler(string queueName)
    {
        if (_eventHandlers.ContainsKey(queueName))
        {
            _eventHandlers.Remove(queueName);
            _logger.LogInformation("Event handler with queue name = {queueName} deleted", queueName);
        }
        else
        {
            throw new QueueNotFoundException($"Cannot delete the queue {queueName} because it does not exist");
        }
    }

    /// <summary>
    /// Forward the event to the right event handler.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="data">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    public Task PushAsync(string queueName, T data, CancellationToken cancellationToken, bool logEvent = true)
    {
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.PushAsync(data, cancellationToken, logEvent);
    }

    /// <summary>
    /// Poll an event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should an event be logged to the log file?</param>
    /// <returns></returns>
    public Task<(T, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true)
    {
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.PollAsync(cancellationToken, logEvent);
    }

    /// <summary>
    /// Ack an event.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">A Guid representing an already polled event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should an event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    public Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true)
    {
        _logger.LogInformation("Event id = {eventId} was successfully processed by the consumer", eventId);
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.AckAsync(eventId, cancellationToken, logEvent);
    }

    /// <summary>
    /// Get an event handler. 
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>An IEventHandler<T></returns>
    public IEventHandler<T> GetEventHandler(string queueName)
    {
        if (_eventHandlers.TryGetValue(queueName, out IEventHandler<T>? eventHandler))
        {
            return eventHandler;
        }

        throw new QueueNotFoundException($"Queue {queueName} does not exist");
    }

    /// <summary>
    /// Check if a queue exists.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>True if the queue exists, False otherwise.</returns>
    public bool ContainsQueue(string queueName)
    {
        return _eventHandlers.ContainsKey(queueName);
    }

    /// <summary>
    /// Get a list of queues.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo[]></returns>
    public Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(_eventHandlers
                                .Select(async pair => await pair.Value.GetQueueInfoAsync(cancellationToken))
                                .ToList());
    }

    /// <summary>
    /// Get queue info.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo></returns>
    public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_eventHandlers.TryGetValue(queueName, out IEventHandler<T>? eventHandler))
        {
            return eventHandler.GetQueueInfoAsync(cancellationToken);
        }

        throw new QueueNotFoundException($"Queue {queueName} does not exist");
    }

    /// <summary>
    /// Trigger a timeout check.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    public async Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken)
    {
        var dateTimeOffset = DateTimeOffset.Now;
        int numberOfTimeouts = 0;

        foreach (KeyValuePair<string, IEventHandler<T>> eventHandler in _eventHandlers)
        {
            numberOfTimeouts += await eventHandler.Value.RequeueTimedOutNackAsync(dateTimeOffset, cancellationToken);
        }

        return numberOfTimeouts;
    }

    /// <summary>
    /// Peek an event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<T></returns>
    public Task<T> PeekAsync(string queueName, CancellationToken cancellationToken)
    {
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.PeekAsync(cancellationToken);
    }

    /// <summary>
    /// Scale number of partitions in a given queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="newNumberOfPartitions">The new number of partitions</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    public async Task ScaleNumberOfPartitions(string queueName, int newNumberOfPartitions, CancellationToken cancellationToken, bool logEvent = true)
    {
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        HashSet<string> toIgnore = eventHandler.GetPartitions().Select(partition => partition.QueueName).ToHashSet();
        await eventHandler.ScaleNumberOfPartitions(newNumberOfPartitions, cancellationToken, logEvent);

        // Should be registered in the event dispatcher.
        eventHandler.GetPartitions()
                    .ForEach(partition =>
                    {
                        if (!toIgnore.Contains(partition.QueueName))
                        {
                            _eventHandlers[partition.QueueName] = partition;
                        }
                    });
    }
}