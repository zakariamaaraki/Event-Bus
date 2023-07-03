using Service_bus.Models;

namespace Service_bus.Services;

/// <summary>
/// Each event handler is mapped to one and only one queue.
/// And handle all operations related to this queue.
/// </summary>
/// <typeparam name="T">The type of the event (subclass of AbstractEvent).</typeparam>
public interface IEventHandler<T>
{
    public int AckTimeout { get; }

    public string QueueName { get; }

    /// <summary>
    /// Get list of partitions.
    /// </summary>
    /// <returns>List of EventHandlers</returns>
    List<IEventHandler<T>> GetPartitions();

    /// <summary>
    /// Scale number of partitions
    /// </summary>
    /// <param name="newNumberOfPartitions">The new number of partitions</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="logEvent">Should the event be logged into the log file?</param>
    /// <returns>A Task.</returns>
    Task ScaleNumberOfPartitions(int newNumberOfPartitions, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Push an event to the queue.
    /// </summary>
    /// <param name="data">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    Task PushAsync(T data, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Poll an event from the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task<(T, Guid)>.</returns>
    Task<(T, Guid)> PollAsync(CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Peek an event from the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<T></returns>
    Task<T> PeekAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ack an event already consumed.
    /// </summary>
    /// <param name="id">The unique id representing an event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    Task AckAsync(Guid id, CancellationToken cancellationToken, bool logEvent = true);

    /// <summary>
    /// Requeue timed out unack events.
    /// </summary>
    /// <param name="dateTimeOffset">The date time offset.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    Task<int> RequeueTimedOutNackAsync(DateTimeOffset dateTimeOffset, CancellationToken cancellationToken);

    /// <summary>
    /// Get the current size of the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int>.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a queue info.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<QueueInfo></returns>
    Task<QueueInfo> GetQueueInfoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get the number of Unacked poll events.
    /// </summary>
    /// <returns>An integer representing the number of unacked poll events.</returns>
    int GetUnAckedPollEvents();

    /// <summary>
    /// Get an event from the storage based on its key.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>The event.</returns>
    T? GetNackEvent(Guid eventId);

    /// <summary>
    /// Try to get an event from the storage based on its key.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="theEvent">The event stored in the nack storage if it exists.</param>
    /// <returns>True if the event exists, false otherwise.</returns>
    bool TryGetNackEvent(Guid eventId, out T? theEvent);

    /// <summary>
    /// Clear the queue from events.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="logEvent">Should the event be logged to the log file?</param>
    /// <returns>A Task.</returns>
    Task Clear(CancellationToken cancellationToken, bool logEvent = true);
}