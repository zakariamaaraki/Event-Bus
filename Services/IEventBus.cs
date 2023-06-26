using Service_bus.Models;

namespace Service_bus.Services;

/// <summary>
/// The front door service to the queueing system. 
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Push an Event to a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="newEvent">The Event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    Task PushEventAsync(string queueName, Event newEvent, CancellationToken cancellationTokenbool, bool logEvent = true);

    /// <summary>
    /// Poll an Event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task<(Event, Guid)></returns>
    Task<(Event, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Peek an event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<Event></returns>
    Task<Event> PeekAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Ack an Event already polled from the queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">A Guid referencing an event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Ack an event and move it to the DLQ.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    Task AckAndMoveToDeadLetterQueue(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Create a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="ackTimeout">The ack timeout for events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the queue?</param>
    /// <param name="numberOfPartitions">The number of partitions.</param>
    /// <returns>A Task.</returns>
    Task CreateQueueAsync(string queueName, int ackTimeout, CancellationToken cancellationToken, bool logEvent = true, int numberOfPartitions = 1);

    /// <summary>
    /// Scale number of partitions in a given queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="newNumberOfPartitions">The new number of partitions</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    Task ScaleNumberOfPartitions(string queueName, int newNumberOfPartitions, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Delete a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Trigger timeout checks.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get list of the queues.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of queue info.</returns>
    Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a queue info.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo></returns>
    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken);
}