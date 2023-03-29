using Service_bus.Models;
using Service_bus.Volumes;
using Service_bus.Headers;
using Service_bus.Storage;

namespace Service_bus.Services;

public class EventHandler<T> : IEventHandler<T> where T : AbstractEvent
{
    private readonly EventQueue<T> _queue;
    private readonly INackStorage<T> _nackStorage;
    private readonly ILogger _logger;
    private readonly IEventLogger<T> _eventLogger;
    private readonly string _queueName;

    public int AckTimeout { get => _nackStorage.AckTimeout; }

    public EventHandler(ILogger logger, IEventLogger<T> eventLogger, int ackTimeout, string queueName)
    {
        _logger = logger;
        _eventLogger = eventLogger;
        _queue = new EventQueue<T>();
        _nackStorage = new NackStorage<T>(ackTimeout);
        _queueName = queueName;
    }

    public async Task PushAsync(T data, CancellationToken cancellationToken, bool logEvent = true)
    {
        await _queue.PushAsync(data, cancellationToken);
        if (logEvent)
        {
            await _eventLogger.LogPushEventAsync(_queueName, data, cancellationToken);
        }
    }

    public async Task<(T, Guid)> PollAsync(CancellationToken cancellationToken, bool logEvent = true)
    {
        T data = await _queue.PollAsync(cancellationToken);
        var eventId = Guid.NewGuid();

        _nackStorage.AddEvent(eventId, data);

        if (logEvent)
        {
            await _eventLogger.LogPollEventAsync(_queueName, eventId, cancellationToken);
        }
        return (data, eventId);
    }

    public Task AckAsync(Guid id, CancellationToken cancellationToken, bool logEvent = true)
    {
        if (_nackStorage.RemoveEvent(id))
        {
            _logger.LogDebug("Record id = {id} successfully deleted from the cache of the queue {queueName}",
                id,
                _queueName);
        }
        else
        {
            _logger.LogDebug(
                "Record id = {id} was not found in the cache of the queue {queueName}",
                id,
                _queueName);
        }

        if (logEvent)
        {
            return _eventLogger.LogAckEventAsync(_queueName, id, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        return _queue.GetCountAsync(cancellationToken);
    }

    public int GetUnAckedPollEvents()
    {
        return _nackStorage.Count();
    }

    public Task<T> PeekAsync(CancellationToken cancellationToken)
    {
        return _queue.PeekAsync(cancellationToken);
    }

    public async Task<QueueInfo> GetQueueInfoAsync(CancellationToken cancellationToken)
    {
        int count = await GetCountAsync(cancellationToken);
        return new QueueInfo()
        {
            QueueName = _queueName,
            AckTimeout = AckTimeout,
            NumberOfElements = count,
            UnAckedPollEvents = GetUnAckedPollEvents()
        };
    }

    public async Task<int> RequeueTimedOutNackAsync(DateTimeOffset dateTimeOffset, CancellationToken cancellationToken)
    {
        List<(Guid, (T, DateTimeOffset))> timedOutEvents = _nackStorage.GetAndRemoveTimedOutEvents(dateTimeOffset);

        foreach ((Guid, (T, DateTimeOffset)) timedOutEvent in timedOutEvents)
        {
            var abstractEvent = (AbstractEvent)timedOutEvent.Item2.Item1;

            Guid eventId = timedOutEvent.Item1;
            DateTimeOffset dateTime = timedOutEvent.Item2.Item2;

            // Check Header values to decide if the event should be requeued or discarded
            if (HeaderHelper.ShouldBeRequeued(abstractEvent.Header))
            {
                // Update event metadata
                int numberOfRetries = HeaderHelper.IncrementCurrentNumberOfAckTimeouts(abstractEvent.Header);
                HeaderHelper.UpdateLastProcessingTimestamp(abstractEvent.Header);

                await PushAsync(timedOutEvent.Item2.Item1, cancellationToken, logEvent: false); // The event should not be logged, otherwise it will be consumed twice during startup!
                _logger.LogInformation($"Timeout occured, the event {eventId} consumed at {dateTime} is requeued to the queue {_queueName} for the {numberOfRetries} time");
            }
            else
            {
                _logger.LogInformation($"Timeout occured, the event {eventId} consumed at {dateTime} will not be requeued to the queue {_queueName}");
            }

        }

        return timedOutEvents.Count;
    }
}