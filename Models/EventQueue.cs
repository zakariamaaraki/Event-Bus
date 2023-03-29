using Service_bus.Exceptions;

namespace Service_bus.Models;

public class EventQueue<T> where T : AbstractEvent
{
    private readonly Queue<T> _queue;

    private readonly SemaphoreSlim _semaphore;

    public EventQueue()
    {
        _queue = new Queue<T>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public Task PushAsync(T data, CancellationToken cancellationToken)
    {
        _semaphore.WaitAsync(cancellationToken);

        _queue.Enqueue(data);

        _semaphore.Release(1);
        return Task.CompletedTask;
    }

    public Task<T> PollAsync(CancellationToken cancellationToken)
    {
        T data;

        _semaphore.WaitAsync(cancellationToken);

        if (_queue.Count < 1)
        {
            throw new NoEventFoundException("The queue is empty");
        }
        data = _queue.Dequeue();

        _semaphore.Release(1);
        return Task.FromResult(data);
    }

    public Task<T> PeekAsync(CancellationToken cancellationToken)
    {
        _semaphore.WaitAsync(cancellationToken);

        T data = _queue.Peek();

        _semaphore.Release(1);
        return Task.FromResult(data);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        _semaphore.WaitAsync(cancellationToken);

        int count = _queue.Count;

        _semaphore.Release(1);
        return Task.FromResult(count);
    }
}