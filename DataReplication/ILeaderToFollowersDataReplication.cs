using Service_bus.Models;

namespace Service_bus.DataReplication;

public interface ILeaderToFollowersDataReplication
{
    Task<bool> PushNewEventAsync(string queueName, Event newEvent, string followerId, CancellationToken cancellationToken);

    Task<bool> PushNewEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken);

    Task<bool> PollEventAsync(string queueName, string followerId, CancellationToken cancellationToken);

    Task<bool> PollEventAsync(string queueName, CancellationToken cancellationToken);

    Task<bool> DeleteQueueAsync(string queueName, string followerId, CancellationToken cancellationToken);

    Task<bool> DeleteQueueAsync(string queueName, CancellationToken cancellationToken);

    Task<bool> ClearQueueAsync(string queueName, string followerId, CancellationToken cancellationToken);

    Task<bool> ClearQueueAsync(string queueName, CancellationToken cancellationToken);

    Task<bool> CreateQueueAsync(string queueName, int numberOfPartitions, int maxAckTimeout, string followerId, CancellationToken cancellationToken);

    Task<bool> CreateQueueAsync(string queueName, int numberOfPartitions, int maxAckTimeout, CancellationToken cancellationToken);

    Task<bool> ScaleNumberOfPartitionsAsync(string queueName, int numberOfPartitions, string followerId, CancellationToken cancellationToken);

    Task<bool> ScaleNumberOfPartitionsAsync(string queueName, int numberOfPartitions, CancellationToken cancellationToken);

    Task<bool> AckEventAsync(string queueName, Guid eventId, string followerId, CancellationToken cancellationToken);

    Task<bool> AckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken);

    Task<bool> AckEventAndMoveItToDLQAsync(string queueName, Guid eventId, string followerId, CancellationToken cancellationToken);

    Task<bool> AckEventAndMoveItToDLQAsync(string queueName, Guid eventId, CancellationToken cancellationToken);
}
