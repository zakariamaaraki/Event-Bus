using Service_bus.Models;

namespace Service_bus.Services;

public interface IEventBus
{
    Task PushEventAsync(string queueName, Event newEvent, CancellationToken cancellationTokenbool, bool logEvent = true);

    Task<(Event, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true);

    Task<Event> PeekAsync(string queueName, CancellationToken cancellationToken);

    Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true);

    Task CreateQueueAsync(string queueName, int ackTimeout, CancellationToken cancellationToken, bool logEvent = true);

    Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true);

    Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken);

    Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken);

    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken);
}