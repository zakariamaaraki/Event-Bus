using Service_bus.Models;

namespace Service_bus.Volumes;

public class EventLogger<T> : IEventLogger<T> where T : AbstractEvent
{
    public const string RecordSeparator = "::";

    // Should be shared between all instances, only one process should write data to log file at a time
    private readonly SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);

    public async Task LogPushEventAsync(string queueName, T pushedEvent, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.Push + ":" + queueName + ":", cancellationToken);
        await FileHelper.WriteDataAsync(pushedEvent.Serialize(), cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    public async Task LogPollEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.Poll + ":" + queueName + ":", cancellationToken);
        await FileHelper.WriteDataAsync(eventId.ToString(), cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    public async Task LogAckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.Ack + ":" + queueName + ":", cancellationToken);
        await FileHelper.WriteDataAsync(eventId.ToString(), cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    public async Task LogQueueCreationEventAsync(string queueName, int ackTimeout, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.CreateQueue + ":" + queueName + ":" + ackTimeout, cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }

    public async Task LogQueueDeletionEventAsync(string queueName, CancellationToken cancellationToken)
    {
        // Note: cancellationToken should not be used inside this method to cancel writing data to log file as it will lead to inconsistency
        await _Semaphore.WaitAsync();
        await FileHelper.WriteDataAsync(EventOperation.DeleteQueue + ":" + queueName, cancellationToken);
        await FileHelper.WriteDataAsync(RecordSeparator, cancellationToken);
        _Semaphore.Release();
    }
}