using Service_bus.Exceptions;

namespace Service_bus.Models;

/// <summary>
/// Event queue Datastructure.
/// </summary>
/// <typeparam name="T">Type of the event (a subclass of AbstractEvent).</typeparam>
public class EventQueue<T> where T : AbstractEvent
{
    private readonly Queue<T> _queue;

    private readonly SemaphoreSlim _semaphore;

    public EventQueue()
    {
        _queue = new Queue<T>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Push the Event into the queue.
    /// </summary>
    /// <param name="data">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    public Task PushAsync(T data, CancellationToken cancellationToken)
    {
        _semaphore.WaitAsync(cancellationToken);

        _queue.Enqueue(data);

        _semaphore.Release(1);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Poll an Event from the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<T>.</returns>
    public Task<T> PollAsync(CancellationToken cancellationToken)
    {
        T data;

        _semaphore.WaitAsync(cancellationToken);

        if (_queue.Count < 1)
        {
            _semaphore.Release(1);
            throw new NoEventFoundException("The queue is empty");
        }
        data = _queue.Dequeue();

        _semaphore.Release(1);
        return Task.FromResult(data);
    }

    /// <summary>
    /// Peek an Event from the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<T>.</returns>
    public Task<T> PeekAsync(CancellationToken cancellationToken)
    {
        _semaphore.WaitAsync(cancellationToken);

        T data = _queue.Peek();

        _semaphore.Release(1);
        return Task.FromResult(data);
    }

    /// <summary>
    /// Get the size of the queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task<int></returns>
    public Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        _semaphore.WaitAsync(cancellationToken);

        int count = _queue.Count;

        _semaphore.Release(1);
        return Task.FromResult(count);
    }
}