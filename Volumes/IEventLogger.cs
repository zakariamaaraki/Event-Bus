using Service_bus.Models;

namespace Service_bus.Volumes;

public interface IEventLogger<T> where T : AbstractEvent
{
    Task LogPushEventAsync(string queueName, T pushedEvent, CancellationToken cancellationToken);

    Task LogPollEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken);

    Task LogAckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken);

    Task LogQueueCreationEventAsync(string queueName, int ackTimeout, CancellationToken cancellationToken);

    Task LogQueueDeletionEventAsync(string queueName, CancellationToken cancellationToken);
}