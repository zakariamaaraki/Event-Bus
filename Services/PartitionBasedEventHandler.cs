using Service_bus.Exceptions;
using Service_bus.Models;
using Service_bus.Volumes;

namespace Service_bus.Services;

public class PartitionBasedEventHandler<T> : IEventHandler<T> where T : AbstractEvent
{
    private readonly List<IEventHandler<T>> _partitions = new List<IEventHandler<T>>();
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly string _queueName;
    private readonly QueueType _queueType;
    private readonly int _ackTimeout;
    private readonly IEventLogger<T> _eventLogger;
    private readonly SemaphoreSlim _readSemaphore = new(1, 1);
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private readonly SemaphoreSlim _peekSemaphore = new(1, 1);
    private int _rebalancingCounterForWrites = -1;
    private int _rebalancingCounterForReads = -1;
    private int _rebalancingCounterForPeeks = -1;

    public PartitionBasedEventHandler(ILogger logger, IEventLogger<T> eventLogger, IEventBus eventBus, int ackTimeout, string queueName, int partitions, QueueType queueType)
    {
        _logger = logger;
        _queueName = queueName;
        _eventLogger = eventLogger;
        _eventBus = eventBus;
        _ackTimeout = ackTimeout;
        _queueType = queueType;

        _logger.LogDebug($"Start creating queue {_queueName} with {partitions} partitions");
        for (int partitionId = 0; partitionId < partitions; partitionId++)
        {
            CreateVirtualQueue(partitionId);
        }
        _logger.LogDebug($"End of creating queue {_queueName} with {partitions} partitions");
    }

    public int AckTimeout => _ackTimeout;

    public string QueueName => _queueName;

    public async Task AckAsync(Guid id, CancellationToken cancellationToken, bool logEvent = true)
    {
        // TODO: Should be optimized (here we are currently doing a brute force)
        for (int partition = 0; partition < _partitions.Count(); partition++)
        {
            await _partitions[partition].AckAsync(id, cancellationToken, logEvent);
        }
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        int[] result = await Task.WhenAll(_partitions
                                                .Select(partition => partition.GetCountAsync(cancellationToken))
                                                .ToList());
        return result.Sum();
    }

    public List<IEventHandler<T>> GetPartitions()
    {
        return _partitions;
    }

    public async Task<QueueInfo> GetQueueInfoAsync(CancellationToken cancellationToken)
    {
        QueueInfo[] queues = await Task.WhenAll(_partitions
                                                    .Select(partition => partition.GetQueueInfoAsync(cancellationToken))
                                                    .ToList());
        Dictionary<string, Partition> partitions = new();
        foreach (QueueInfo queueInfo in queues)
        {
            partitions.Add(queueInfo.QueueName, queueInfo.Partitions[queueInfo.QueueName]);
        }

        return new QueueInfo
        {
            AckTimeout = _ackTimeout,
            NumberOfPartitions = _partitions.Count(),
            QueueName = _queueName,
            Type = _queueType,
            Partitions = partitions
        };
    }

    public int GetUnAckedPollEvents()
    {
        return _partitions.Select(partition => partition.GetUnAckedPollEvents()).Sum();
    }

    public async Task<T> PeekAsync(CancellationToken cancellationToken)
    {
        for (int partitionId = 0; partitionId < _partitions.Count(); partitionId++)
        {
            int partition = GetPeekPartition(cancellationToken);
            try
            {
                return await _partitions[partition].PeekAsync(cancellationToken);
            }
            catch (NoEventFoundException)
            {
                // Poll from other partitions
            }
        }

        throw new NoEventFoundException($"The queue {_queueName} is empty, all partitions are empty");
    }

    public async Task<(T, Guid)> PollAsync(CancellationToken cancellationToken, bool logEvent = true)
    {
        for (int partitionId = 0; partitionId < _partitions.Count(); partitionId++)
        {
            int partition = GetReadPartition(cancellationToken);
            try
            {
                return await _partitions[partition].PollAsync(cancellationToken, logEvent);
            }
            catch (NoEventFoundException)
            {
                // Poll from other partitions
            }
        }

        throw new NoEventFoundException($"The queue {_queueName} is empty, all partitions are empty");
    }

    public async Task PushAsync(T data, CancellationToken cancellationToken, bool logEvent = true)
    {
        int partition = GetWritePartition(cancellationToken);
        await _partitions[partition].PushAsync(data, cancellationToken, logEvent);
    }

    public async Task<int> RequeueTimedOutNackAsync(DateTimeOffset dateTimeOffset, CancellationToken cancellationToken)
    {
        int[] result = await Task.WhenAll(_partitions
                                            .Select(partition => partition.RequeueTimedOutNackAsync(dateTimeOffset, cancellationToken))
                                            .ToList());
        return result.Sum();
    }

    public async Task ScaleNumberOfPartitions(int newNumberOfPartitions, CancellationToken cancellationToken, bool logEvent = true)
    {
        int currentNumberOfPartitions = _partitions.Count();
        if (newNumberOfPartitions <= currentNumberOfPartitions)
        {
            throw new ServiceBusInvalidOperationException($"You can only increase the number of partitions,"
                                                            + $" current number of partitions = {currentNumberOfPartitions},"
                                                            + $" requested number of partitions = {newNumberOfPartitions}");
        }
        for (int i = 0; i < newNumberOfPartitions - currentNumberOfPartitions; i++)
        {
            CreateVirtualQueue(currentNumberOfPartitions + i);
        }
        if (logEvent)
        {
            await _eventLogger.LogScaleNumberOfPartitionsEventAsync(_queueName, newNumberOfPartitions, cancellationToken);
        }
    }

    public T? GetNackEvent(Guid eventId)
    {
        foreach (IEventHandler<T> partition in _partitions)
        {
            if (partition.TryGetNackEvent(eventId, out T? theEvent))
            {
                return theEvent;
            }
        }
        throw new EventNotFoundException("The event was nout found in the nack storage");
    }

    public bool TryGetNackEvent(Guid eventId, out T? theEvent)
    {
        theEvent = null;
        foreach (IEventHandler<T> partition in _partitions)
        {
            if (partition.TryGetNackEvent(eventId, out T? eventFound))
            {
                theEvent = eventFound;
                return true;
            }
        }
        return false;
    }

    private int GetPeekPartition(CancellationToken cancellationToken)
    {
        _peekSemaphore.WaitAsync(cancellationToken);
        try
        {
            int partition = _rebalancingCounterForPeeks = (_rebalancingCounterForPeeks + 1) % _partitions.Count();
            return partition;
        }
        finally
        {
            _peekSemaphore.Release(1);
        }
    }

    private int GetReadPartition(CancellationToken cancellationToken)
    {
        _readSemaphore.WaitAsync(cancellationToken);
        try
        {
            int partition = _rebalancingCounterForReads = (_rebalancingCounterForReads + 1) % _partitions.Count();
            return partition;
        }
        finally
        {
            _readSemaphore.Release(1);
        }
    }

    private int GetWritePartition(CancellationToken cancellationToken)
    {
        _writeSemaphore.WaitAsync(cancellationToken);
        try
        {
            int partition = _rebalancingCounterForReads = (_rebalancingCounterForReads + 1) % _partitions.Count();
            return partition;
        }
        finally
        {
            _writeSemaphore.Release(1);
        }
    }

    private void CreateVirtualQueue(int partitionId)
    {
        string partitionName = $"{_queueName}-partition${partitionId}";
        _logger.LogInformation($"Creating new virtual partition {partitionName} for the queue {_queueName}");
        var eventHandler = new EventHandler<T>(_logger, _eventLogger, _eventBus, _ackTimeout, partitionName, QueueType.Partition);
        _partitions.Add(eventHandler);
    }

    public async Task Clear(CancellationToken cancellationToken, bool logEvent = true)
    {
        _logger.LogDebug($"Start clearing the queue {QueueName}");
        for (int partitionId = 0; partitionId < _partitions.Count(); partitionId++)
        {
            await _partitions[partitionId].Clear(cancellationToken, logEvent);
        }
        _logger.LogDebug($"End clearing the queue {QueueName}");
    }
}
