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
}