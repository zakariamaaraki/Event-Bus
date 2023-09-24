using Service_bus.Models;

namespace Service_bus.DataReplication;

public interface ILeaderToFollowersDataReplication
{
    Task<bool> PushNewEventAsync(string queueName, Event newEvent, string followerId, CancellationToken cancellationToken);

    Task<bool> PushNewEventAsync(string queueName, Event newEvent, CancellationToken cancellationToken);

    Task<bool> CreateQueueAsync(string queueName, int numberOfPartitions, int maxAckTimeout, string followerId, CancellationToken cancellationToken);

    Task<bool> CreateQueueAsync(string queueName, int numberOfPartitions, int maxAckTimeout, CancellationToken cancellationToken);
}
