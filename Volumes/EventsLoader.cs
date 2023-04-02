using Service_bus.Services;
using Service_bus.Models;

namespace Service_bus.Volumes;

/// <summary>
/// Load all events from log files.
/// </summary>
public class EventsLoader : IEventsLoader
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventsLoader> _logger;

    private const int DefaultAckTimeout = 30;

    public EventsLoader(IEventBus eventBus, ILogger<EventsLoader> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Load events from log files.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    public async Task Load(CancellationToken cancellationToken)
    {
        IEnumerable<(string, Task<string>)> Readers = FileHelper.ReadDataAsync();

        foreach ((string fileName, Task<string> reader) in Readers)
        {
            string data = await reader;
            string[] events = data.Split(EventLogger<Event>.RecordSeparator);
            foreach (string newEvent in events)
            {
                if (newEvent.Length <= 1)
                {
                    continue;
                }

                string[] operation = newEvent.Split(":");
                var eventOperation = (EventOperation)Enum.Parse(typeof(EventOperation), operation[0]);

                switch (eventOperation)
                {
                    case EventOperation.Push:
                        await PushEventAsync(operation, cancellationToken);
                        break;
                    case EventOperation.Poll:
                        await PollEventAsync(operation, cancellationToken);
                        break;
                    case EventOperation.Ack:
                        await AckEventAsync(operation, cancellationToken);
                        break;
                    case EventOperation.CreateQueue:
                        await CreateQueueAsync(operation, cancellationToken);
                        break;
                    case EventOperation.DeleteQueue:
                        await DeleteQueueAsync(operation, cancellationToken);
                        break;
                    default:
                        _logger.LogError($"Unknown operation found in the log file: {fileName}");
                        break;
                }
            }
        }
    }

    private Task DeleteQueueAsync(string[] operation, CancellationToken cancellationToken)
    {
        return _eventBus.DeleteQueueAsync(operation[1], cancellationToken, logEvent: false);
    }

    private Task CreateQueueAsync(string[] operation, CancellationToken cancellationToken)
    {
        if (Int32.TryParse(operation[2], out int ackTimeout))
        {
            return _eventBus.CreateQueueAsync(operation[1], ackTimeout, cancellationToken, logEvent: false);
        }
        _logger.LogError($"Cannot Parse the integer ack timeout {operation[2]}"
                        + $"the queue, will be created with a default timeout of {DefaultAckTimeout} min");
        return _eventBus.CreateQueueAsync(operation[1], DefaultAckTimeout, cancellationToken, logEvent: false);
    }

    private Task AckEventAsync(string[] operation, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(operation[2], out Guid eventId))
        {
            return _eventBus.AckAsync(operation[1], eventId, cancellationToken, logEvent: false);
        }
        _logger.LogError($"Cannot Parse the Guid eventId {operation[2]}, the event will not be acked");
        return Task.CompletedTask;
    }

    private Task PollEventAsync(string[] operation, CancellationToken cancellationToken)
    {
        return _eventBus.PollAsync(operation[1], cancellationToken, logEvent: false);
    }

    private Task PushEventAsync(string[] operation, CancellationToken cancellationToken)
    {
        return _eventBus.PushEventAsync(operation[1], new Event().Deserialize(operation[2]), cancellationToken, logEvent: false);
    }
}