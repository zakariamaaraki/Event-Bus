using Service_bus.Models;

namespace Service_bus.Volumes;

/// <summary>
/// Used to log events.
/// </summary>
/// <typeparam name="T">The type of the events (a subclass of AbstractEvent).</typeparam>
public interface IEventLogger<T> where T : AbstractEvent
{
    /// <summary>
    /// Log push events.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="pushedEvent">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    Task LogPushEventAsync(string queueName, T pushedEvent, CancellationToken cancellationToken);

    /// <summary>
    /// Log poll event.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    Task LogPollEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Log ack event.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    Task LogAckEventAsync(string queueName, Guid eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Log create a queue event.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="numberOfPartitions">Number of partitions.</param>
    /// <param name="ackTimeout">The ack timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    Task LogQueueCreationEventAsync(string queueName, int numberOfPartitions, int ackTimeout, CancellationToken cancellationToken);

    /// <summary>
    /// Scale the number of partitions in a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="newNumberOfPartitions">The new number of partitions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    Task LogScaleNumberOfPartitionsEventAsync(string queueName, int newNumberOfPartitions, CancellationToken cancellationToken);

    /// <summary>
    /// Log delete queue event.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    Task LogQueueDeletionEventAsync(string queueName, CancellationToken cancellationToken);
}