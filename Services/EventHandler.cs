using Service_bus.Models;
using Service_bus.Volumes;
using Service_bus.Headers;
using Service_bus.Storage;
using Service_bus.Exceptions;

namespace Service_bus.Services;

/// <summary>
/// Each event handler is mapped to one and only one queue.
/// And handle all operations related to this queue.
/// </summary>
/// <typeparam name="T">The type of the event (subclass of AbstractEvent).</typeparam>
public class EventHandler<T> : IEventHandler<T> where T : AbstractEvent
{
    private readonly EventQueue<T> _queue;
    private readonly INackStorage<T> _nackStorage;
    private readonly ILogger _logger;
    private readonly IEventLogger<T> _eventLogger;
    private readonly string _queueName;
    private readonly QueueType _queueType;
    private readonly IEventBus _eventBus;

    public int AckTimeout { get => _nackStorage.AckTimeout; }
    public string QueueName { get => _queueName; }

    public EventHandler(ILogger logger, IEventLogger<T> eventLogger, IEventBus eventBus, int ackTimeout, string queueName, QueueType queueType)
    {
        _logger = logger;
        _eventLogger = eventLogger;
        _eventBus = eventBus;
        _queue = new EventQueue<T>();
        _nackStorage = new NackStorage<T>(ackTimeout);
        _queueName = queueName;
        _queueType = queueType;
    }

    /// <summary>
    /// Push an event to the queue.
    /// </summary>
    /// <param name="data">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    public async Task PushAsync(T data, CancellationToken cancellationToken, bool logEvent = true)
    {
        await _queue.PushAsync(data, cancellationToken);
        if (logEvent)
        {
            await _eventLogger.LogPushEventAsync(_queueName, data, cancellationToken);
        }
    }

    /// <summary>
    /// Poll an event from the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task<(T, Guid)>.</returns>
    public async Task<(T, Guid)> PollAsync(CancellationToken cancellationToken, bool logEvent = true)
    {
        T data;
        try
        {
            data = await _queue.PollAsync(cancellationToken);
        }
        catch (NoEventFoundException e)
        {
            _logger.LogInformation($"The queue {QueueName} is empty");
            throw e;
        }

        var eventId = Guid.NewGuid();
        _nackStorage.AddEvent(eventId, data);

        if (logEvent)
        {
            await _eventLogger.LogPollEventAsync(_queueName, eventId, cancellationToken);
        }
        return (data, eventId);
    }

    /// <summary>
    /// Ack an event already consumed.
    /// </summary>
    /// <param name="id">The unique id representing an event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    public async Task AckAsync(Guid id, CancellationToken cancellationToken, bool logEvent = true)
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
            await _eventLogger.LogAckEventAsync(_queueName, id, cancellationToken);
        }
    }

    /// <summary>
    /// Get the current size of the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int>.</returns>
    public Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        return _queue.GetCountAsync(cancellationToken);
    }

    /// <summary>
    /// Get the number of Unacked poll events.
    /// </summary>
    /// <returns>An integer representing the number of unacked poll events.</returns>
    public int GetUnAckedPollEvents()
    {
        return _nackStorage.Count();
    }

    /// <summary>
    /// Peek an event from the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<T></returns>
    public Task<T> PeekAsync(CancellationToken cancellationToken)
    {
        return _queue.PeekAsync(cancellationToken);
    }

    /// <summary>
    /// Get a queue info.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo></returns>
    public async Task<QueueInfo> GetQueueInfoAsync(CancellationToken cancellationToken)
    {
        int count = await GetCountAsync(cancellationToken);
        return new QueueInfo()
        {
            QueueName = _queueName,
            AckTimeout = AckTimeout,
            NumberOfPartitions = 1,
            Type = _queueType,
            Partitions = new Dictionary<string, Partition>() {
                {
                    _queueName,
                    new Partition() {
                        NumberOfElements = count,
                        UnAckedPollEvents = GetUnAckedPollEvents()
                    }
                }
            }
        };
    }

    /// <summary>
    /// Requeue timed out unack events.
    /// </summary>
    /// <param name="dateTimeOffset">The date time offset.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    public async Task<int> RequeueTimedOutNackAsync(DateTimeOffset dateTimeOffset, CancellationToken cancellationToken)
    {
        List<(Guid, (T, DateTimeOffset))> timedOutEvents = _nackStorage.GetTimedOutEvents(dateTimeOffset);

        foreach ((Guid, (T, DateTimeOffset)) timedOutEvent in timedOutEvents)
        {
            var abstractEvent = (AbstractEvent)timedOutEvent.Item2.Item1;

            Guid eventId = timedOutEvent.Item1;
            DateTimeOffset dateTime = timedOutEvent.Item2.Item2;

            // Check if the event should be sent to DLQ
            if (_queueType == QueueType.Queue && HeaderHelper.ShouldBeSentToDLQ(abstractEvent.Header))
            {
                _logger.LogInformation($"Timeout occured, the event {eventId} consumed at {dateTime} will be sent to the DLQ");
                HeaderHelper.UpdateSendToDeadLetterQueueAfterAckTimeoutHeaderValue(abstractEvent.Header, false);
                await _eventBus.AckAndMoveToDeadLetterQueue(QueueName, eventId, cancellationToken, logEvent: true);
            }
            else
            {
                _nackStorage.RemoveEvent(timedOutEvent.Item1);

                // Check Header values to decide if the event should be requeued or discarded
                if (HeaderHelper.ShouldBeRequeued(abstractEvent.Header))
                {
                    // Update event metadata
                    int numberOfRetries = HeaderHelper.IncrementCurrentNumberOfAckTimeouts(abstractEvent.Header);
                    HeaderHelper.UpdateLastProcessingTimestamp(abstractEvent.Header);

                    _logger.LogInformation($"Timeout occured, the event {eventId} consumed at {dateTime} will be requeued to the queue {_queueName} for the {numberOfRetries} time");
                    await PushAsync(timedOutEvent.Item2.Item1, cancellationToken, logEvent: false); // The event should not be logged, otherwise it will be consumed twice during startup!
                }
                else
                {
                    _logger.LogInformation($"Timeout occured, the event {eventId} consumed at {dateTime} will not be requeued to the queue {_queueName}");
                }
            }
        }

        return timedOutEvents.Count;
    }

    public T? GetNackEvent(Guid eventId)
    {
        return _nackStorage.GetEvent(eventId);
    }

    public bool TryGetNackEvent(Guid eventId, out T? theEvent)
    {
        theEvent = null;
        if (_nackStorage.ContainsEvent(eventId))
        {
            theEvent = _nackStorage.GetEvent(eventId);
            return true;
        }
        return false;
    }

    public List<IEventHandler<T>> GetPartitions()
    {
        return new List<IEventHandler<T>>();
    }

    public Task ScaleNumberOfPartitions(int newNumberOfPartitions, CancellationToken cancellationToken, bool logEvent = true)
    {
        throw new ServiceBusInvalidOperationException("You can not scale a queue created without partitions");
    }

    public async Task Clear(CancellationToken cancellationToken, bool logEvent = true)
    {
        _logger.LogInformation($"Clearing the queue {QueueName}");
        await _queue.Clear(cancellationToken);
        if (logEvent)
        {
            await _eventLogger.LogQueueClearingEventAsync(QueueName, cancellationToken);
        }
    }
}