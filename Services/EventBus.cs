using Service_bus.Models;
using Service_bus.Exceptions;
using Service_bus.Configurations;
using Service_bus.Volumes;

using Microsoft.Extensions.Options;
using InvalidOperationException = Service_bus.Exceptions.InvalidOperationException;
using Service_bus.DataReplication;

namespace Service_bus.Services;

/// <summary>
/// The front door service to the queueing system. 
/// </summary>
public class EventBus : IEventBus
{
    private readonly IEventDispatcher<Event> _eventDispatcher;
    private readonly ILogger<EventBus> _logger;
    private readonly IEventLogger<Event> _eventLogger;
    private readonly ILeaderToFollowersDataReplication _leaderToFollowersDataReplication;
    private readonly int _maxKeySize;
    private readonly int _maxBodySize;

    private const int MaxAckTimeout = 60 * 24; // 1 day
    private const int MaxQueueNameLength = 100;
    private const int MaxPartitions = 100;
    private const string DeadLetterQueueSuffix = "-DLQ";

    public EventBus(
        IEventDispatcher<Event> eventDispatcher,
        ILogger<EventBus> logger,
        IEventLogger<Event> eventLogger,
        ILeaderToFollowersDataReplication leaderToFollowersDataReplication,
        IOptions<EventOptions> eventOptions)
    {
        _eventDispatcher = eventDispatcher;
        _logger = logger;
        _eventLogger = eventLogger;
        _leaderToFollowersDataReplication = leaderToFollowersDataReplication;
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
    public async Task PushEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken, bool logEvent = true)
    {
        ValidateEvent(newEvent);
        _logger.LogInformation("Pushing new event into the queue {queueName}", queueName);

        await _eventDispatcher.PushAsync(queueName, newEvent, cancellationToken, logEvent);

        if (logEvent)
        {
            // TODO: add check if the replication went correctely.
            await _leaderToFollowersDataReplication.PushNewEventAsync(queueName, newEvent, cancellationToken);
        }
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

        // Fire and forget, to make read operation fast.
        _ = Task.Run(async () =>
        {
            if (logEvent)
            {
                // TODO: add check if the replication went correctely.
                await _leaderToFollowersDataReplication.PollEventAsync(queueName, cancellationToken);
            }
        });

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
    public async Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true)
    {
        await _eventDispatcher.AckAsync(queueName, eventId, cancellationToken, logEvent);

        if (logEvent)
        {
            // TODO: add check if the replication went correctely.
            await _leaderToFollowersDataReplication.AckEventAsync(queueName, eventId, cancellationToken);
        }
    }

    /// <summary>
    /// Ack an event and move it to the DLQ.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    public async Task AckAndMoveToDeadLetterQueue(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true)
    {
        // Get the type of the queue for validation
        // We should only allow calls when queue type is equals to queue
        (_, QueueType queueType) = _eventDispatcher.GetEventHandler(queueName);
        if (queueType != QueueType.Queue)
        {
            throw new InvalidOperationException($"No dead letter queue is attached to this {queueType.ToString()}: {queueName}");
        }

        string deadLetterQueueName = GetDeadLetterQueueName(queueName);
        _logger.LogInformation($"Event: {eventId} from the queue: {queueName} will be moved to DLQ: {deadLetterQueueName}");
        Event? nackEvent = _eventDispatcher.GetNackEvent(queueName, eventId);

        if (nackEvent is not null)
        {
            // Send the event to the DLQ
            await _eventDispatcher.PushAsync(deadLetterQueueName, nackEvent, cancellationToken, logEvent);
        }
        else
        {
            _logger.LogWarning("Nack event is not supposed to be null, the event will not be sent to the DLQ");
        }

        // Ack the event
        // For atomicity concerns the cancellation token should not be evaluated while acking the event.
        await AckAsync(queueName, eventId, CancellationToken.None, logEvent);

        if (logEvent)
        {
            // TODO: add check if the replication went correctely.
            await _leaderToFollowersDataReplication.AckEventAndMoveItToDLQAsync(queueName, eventId, cancellationToken);
        }
    }

    /// <summary>
    /// Create a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="ackTimeout">The ack timeout for events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the queue?</param>
    /// <param name="numberOfPartitions">The number of partitions.</param>
    /// <returns>A Task.</returns>
    public async Task CreateQueueAsync(
        string queueName,
        int ackTimeout,
        CancellationToken cancellationToken,
        bool logEvent = true,
        int numberOfPartitions = 1)
    {
        await CreateQueueAsync(queueName, ackTimeout, cancellationToken, isDLQ: false, logEvent: logEvent, numberOfPartitions: numberOfPartitions);

        if (logEvent)
        {
            // TODO: add check if the replication went correctely.
            await _leaderToFollowersDataReplication.CreateQueueAsync(queueName, numberOfPartitions, ackTimeout, cancellationToken);
        }
    }

    private async Task CreateQueueAsync(
        string queueName,
        int ackTimeout,
        CancellationToken cancellationToken,
        bool isDLQ = false,
        bool logEvent = true,
        int numberOfPartitions = 1)
    {
        ValidateArguments(queueName, ackTimeout, numberOfPartitions);

        IEventHandler<Event> eventHandler;
        QueueType queueType = isDLQ ? QueueType.DeadLetterQueue : QueueType.Queue;

        if (numberOfPartitions > 1)
        {
            eventHandler = new PartitionBasedEventHandler<Event>(_logger, _eventLogger, this, ackTimeout, queueName, numberOfPartitions, queueType);

            // We should also register all partitions, for fast access to partitions, typically during startup.
            foreach (IEventHandler<Event> partition in eventHandler.GetPartitions())
            {
                _eventDispatcher.AddEventHandler(partition.QueueName, partition, QueueType.Partition);
            }
        }
        else
        {
            eventHandler = new EventHandler<Event>(_logger, _eventLogger, this, ackTimeout, queueName, queueType);
        }
        _eventDispatcher.AddEventHandler(queueName, eventHandler, queueType);

        if (!isDLQ)
        {
            // Create the correspending DLQ queues.
            await CreateQueueAsync(GetDeadLetterQueueName(queueName), ackTimeout, cancellationToken, isDLQ: true, logEvent: false, numberOfPartitions: numberOfPartitions);
        }

        if (logEvent)
        {
            await _eventLogger.LogQueueCreationEventAsync(queueName, numberOfPartitions, ackTimeout, cancellationToken);
        }
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
        (_, QueueType queueType) = _eventDispatcher.GetEventHandler(queueName);

        // Scaling should be allowed only for QueueType = Queue
        if (queueType != QueueType.Queue)
        {
            throw new InvalidOperationException("Scaling can be done only for queues");
        }

        await ScaleNumberOfPartitions(queueName, newNumberOfPartitions, cancellationToken, isDLQ: false, logEvent: logEvent);

        if (logEvent)
        {
            // TODO: add check if the replication went correctely.
            await _leaderToFollowersDataReplication.ScaleNumberOfPartitionsAsync(queueName, newNumberOfPartitions, cancellationToken);
        }
    }

    private async Task ScaleNumberOfPartitions(
        string queueName,
        int newNumberOfPartitions,
        CancellationToken cancellationToken,
        bool isDLQ = false,
        bool logEvent = true)
    {
        if (newNumberOfPartitions < 1 || newNumberOfPartitions > MaxPartitions)
        {
            throw new InvalidArgumentException($"number of partitions should be between 1 and ${MaxPartitions}");
        }

        await _eventDispatcher.ScaleNumberOfPartitions(queueName, newNumberOfPartitions, cancellationToken, logEvent);

        if (!isDLQ)
        {
            await ScaleNumberOfPartitions(GetDeadLetterQueueName(queueName), newNumberOfPartitions, cancellationToken, isDLQ: true, logEvent: false);
        }
    }

    /// <summary>
    /// Delete a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    public async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true)
    {
        if (string.IsNullOrEmpty(queueName))
        {
            throw new InvalidArgumentException("Queue name cannot be null or empty");
        }
        (_, QueueType queueType) = _eventDispatcher.GetEventHandler(queueName);

        switch (queueType)
        {
            case QueueType.DeadLetterQueue:
                throw new InvalidOperationException("You cannot directly delete a dead letter queue");
            case QueueType.Partition:
                throw new InvalidOperationException("You cannot delete a partition");
        }

        await DeleteQueueAsync(queueName, cancellationToken, isDLQ: false, logEvent: logEvent);

        if (logEvent)
        {
            // TODO: add check if the replication went correctely.
            await _leaderToFollowersDataReplication.DeleteQueueAsync(queueName, cancellationToken);
        }
    }

    private async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken, bool isDLQ = false, bool logEvent = true)
    {
        (IEventHandler<Event> eventHandler, _) = _eventDispatcher.GetEventHandler(queueName);

        // Delete also all partitions and the correspending DLQ.
        foreach (IEventHandler<Event> partition in eventHandler.GetPartitions())
        {
            _eventDispatcher.RemoveEventHandler(partition.QueueName);
        }
        _eventDispatcher.RemoveEventHandler(queueName);

        if (!isDLQ)
        {
            // Delete the DLQ and its partitions
            await DeleteQueueAsync(GetDeadLetterQueueName(queueName), cancellationToken, isDLQ: true, logEvent: false);
        }

        if (logEvent)
        {
            await _eventLogger.LogQueueDeletionEventAsync(queueName, cancellationToken);
        }
    }

    private static void ValidateArguments(string queueName, int ackTimeout, int numberOfPartitions)
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
        if (numberOfPartitions <= 0 || numberOfPartitions > MaxPartitions)
        {
            throw new InvalidArgumentException($"number of partitions should be between 1 and ${MaxPartitions}");
        }
    }

    /// <summary>
    /// Get list of the queues.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of queue info.</returns>
    public async Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken)
    {
        return await _eventDispatcher.GetListOfQueuesAsync(cancellationToken);
    }

    /// <summary>
    /// Get a queue info.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo></returns>
    public async Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        return await _eventDispatcher.GetQueueInfoAsync(queueName, cancellationToken);
    }

    /// <summary>
    /// Peek an event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<Event></returns>
    public async Task<Event> PeekAsync(string queueName, CancellationToken cancellationToken)
    {
        return await _eventDispatcher.PeekAsync(queueName, cancellationToken);
    }

    /// <summary>
    /// Trigger timeout checks.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    public async Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken)
    {
        return await _eventDispatcher.TriggerTimeoutChecksAsync(cancellationToken);
    }

    public async Task Clear(string queueName, CancellationToken cancellationToken, bool logEvent = true)
    {
        await _eventDispatcher.Clear(queueName, cancellationToken, logEvent);

        if (logEvent)
        {
            // TODO: add check if the replication went correctely.
            await _leaderToFollowersDataReplication.ClearQueueAsync(queueName, cancellationToken);
        }
    }

    private string GetDeadLetterQueueName(string queueName)
    {
        return $"{queueName}{DeadLetterQueueSuffix}";
    }
}