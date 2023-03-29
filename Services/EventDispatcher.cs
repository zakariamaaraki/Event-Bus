using Service_bus.Models;
using Service_bus.Exceptions;

namespace Service_bus.Services;

public class EventDispatcher<T> : IEventDispatcher<T> where T : AbstractEvent
{
    private readonly Dictionary<string, IEventHandler<T>> _eventHandlers = new();
    private readonly ILogger<EventDispatcher<T>> _logger;

    public EventDispatcher(ILogger<EventDispatcher<T>> logger)
    {
        _logger = logger;
    }

    public void AddEventHandler(string queueName, IEventHandler<T> eventHandler)
    {
        if (_eventHandlers.ContainsKey(queueName))
        {
            throw new QueueAlreadyExistsException($"Queue {queueName} already exists, please provide another name");
        }
        _eventHandlers[queueName] = eventHandler;
        _logger.LogInformation("New event handler registered with queue name = {queueName}", queueName);
    }

    public void RemoveEventHandler(string queueName)
    {
        if (_eventHandlers.ContainsKey(queueName))
        {
            _eventHandlers.Remove(queueName);
            _logger.LogInformation("Event handler with queue name = {queueName} removed", queueName);
        }
        else
        {
            throw new QueueNotFoundException($"Cannot delete the queue {queueName} because it does not exist");
        }
    }

    public Task PushAsync(string queueName, T data, CancellationToken cancellationToken, bool logEvent = true)
    {
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.PushAsync(data, cancellationToken, logEvent);
    }

    public Task<(T, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true)
    {
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.PollAsync(cancellationToken, logEvent);
    }

    public Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true)
    {
        _logger.LogInformation("Event id = {eventId} was successfully processed by the consumer", eventId);
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.AckAsync(eventId, cancellationToken, logEvent);
    }

    public IEventHandler<T> GetEventHandler(string queueName)
    {
        if (_eventHandlers.TryGetValue(queueName, out IEventHandler<T>? eventHandler))
        {
            return eventHandler;
        }

        throw new QueueNotFoundException($"Queue {queueName} does not exist");
    }

    public bool ContainsQueue(string queueName)
    {
        return _eventHandlers.ContainsKey(queueName);
    }

    public Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(_eventHandlers
                        .Select(async pair => await pair.Value.GetQueueInfoAsync(cancellationToken))
                        .ToList());
    }

    public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_eventHandlers.TryGetValue(queueName, out IEventHandler<T>? eventHandler))
        {
            return eventHandler.GetQueueInfoAsync(cancellationToken);
        }

        throw new QueueNotFoundException($"Queue {queueName} does not exist");
    }

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

    public Task<T> PeekAsync(string queueName, CancellationToken cancellationToken)
    {
        IEventHandler<T> eventHandler = GetEventHandler(queueName);
        return eventHandler.PeekAsync(cancellationToken);
    }
}