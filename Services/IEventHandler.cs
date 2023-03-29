using Service_bus.Models;

namespace Service_bus.Services;

public interface IEventHandler<T>
{
    public int AckTimeout { get; }

    Task PushAsync(T data, CancellationToken cancellationToken, bool logEvent = true);

    Task<(T, Guid)> PollAsync(CancellationToken cancellationToken, bool logEvent = true);

    Task<T> PeekAsync(CancellationToken cancellationToken);

    Task AckAsync(Guid id, CancellationToken cancellationToken, bool logEvent = true);

    Task<int> RequeueTimedOutNackAsync(DateTimeOffset dateTimeOffset, CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<QueueInfo> GetQueueInfoAsync(CancellationToken cancellationToken);

    int GetUnAckedPollEvents();
}