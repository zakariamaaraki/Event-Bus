using Service_bus.Models;

namespace Service_bus.Volumes;

/// <summary>
/// Used to log events to a log file.
/// </summary>
/// <typeparam name="T">The type of the events (a subclass of AbstractEvent).</typeparam>
public class EventLogger<T> : IEventLogger<T> where T : AbstractEvent
{
    public const string RecordSeparator = "::";

    // Should be shared between all instances, only one process should write data to log file at a time
    private readonly SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Log push events to a log file.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="pushedEvent">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    public async Task LogPushEventAsync(string queueName, T pushedEvent, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.Push + ":" + queueName + ":", cancellationToken);
        await FileHelper.WriteDataAsync(pushedEvent.Serialize(), cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    /// <summary>
    /// Log poll event to a log file.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    public async Task LogPollEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.Poll + ":" + queueName + ":", cancellationToken);
        await FileHelper.WriteDataAsync(eventId.ToString(), cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    /// <summary>
    /// Log ack event to a log file.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    public async Task LogAckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.Ack + ":" + queueName + ":", cancellationToken);
        await FileHelper.WriteDataAsync(eventId.ToString(), cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    /// <summary>
    /// Log create queue event to a log file.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="ackTimeout">The ack timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    public async Task LogQueueCreationEventAsync(string queueName, int ackTimeout, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.CreateQueue + ":" + queueName + ":" + ackTimeout, cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    /// <summary>
    /// Log delete queue event to a log file.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    public async Task LogQueueDeletionEventAsync(string queueName, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.DeleteQueue + ":" + queueName, cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }
}