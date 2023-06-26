using Service_bus.Models;

namespace Service_bus.Services;

/// <summary>
/// Orchestrates events to the right event handler. 
/// </summary>
/// <typeparam name="T">The tipe of Event (subclass of AbstractEvent).</typeparam>
public interface IEventDispatcher<T>
{
    /// <summary>
    /// Add an event handler to the internal Dictionary.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventHandler">The event handler.</param>
    /// <param name="queueType">The type of the queue.</param>
    void AddEventHandler(string queueName, IEventHandler<T> eventHandler, QueueType queueType);

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
    /// Remove an event handler from the internal Dictionary.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    void RemoveEventHandler(string queueName);

    /// <summary>
    /// Get an event handler. 
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>An IEventHandler<T> and the queue type</returns>
    (IEventHandler<T>, QueueType) GetEventHandler(string queueName);

    /// <summary>
    /// Forward the event to the right event handler.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="data">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    Task PushAsync(string queueName, T data, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Poll an event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should an event be logged to the log file?</param>
    /// <returns></returns>
    Task<(T, Guid)> PollAsync(string queueName, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Peek an event from a queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<T></returns>
    Task<T> PeekAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Ack an event.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">A Guid representing an already polled event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should an event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    Task AckAsync(string queueName, Guid eventId, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Get nack event, based on the queue name and the event id.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="eventId">The event id.</param>
    /// <returns>The event.</returns>
    T? GetNackEvent(string queueName, Guid eventId);

    /// <summary>
    /// Check if a queue exists.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>True if the queue exists, False otherwise.</returns>
    bool ContainsQueue(string queueName);

    /// <summary>
    /// Get a list of queues.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo[]></returns>
    Task<QueueInfo[]> GetListOfQueuesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get queue info.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo></returns>
    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Trigger a timeout check.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    Task<int> TriggerTimeoutChecksAsync(CancellationToken cancellationToken);
}