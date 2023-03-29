using Service_bus.Models;

namespace Service_bus.Services;

public interface IEventDispatcher<T>
{
    void AddEventHandler(string queueName, IEventHandler<T> eventHandler);

    void RemoveEventHandler(string queueName);

    IEventHandler<T> GetEventHandler(string queueName);

    Task PushAsync(string queueName, T data, CancellationToken cancellationToken, bool logEvent = true);

    Task<(T, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true);

    Task<T> PeekAsync(string queueName, CancellationToken cancellationToken);

    Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true);

    bool ContainsQueue(string queueName);

    Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken);

    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken);

    Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken);
}