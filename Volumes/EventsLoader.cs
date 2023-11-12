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
    /// <returns>A Task.</returns>sss
    public async Task Load(CancellationToken cancellationToken)
    {
        IAsyncEnumerable<(string, string)> Readers = FileHelper.ReadDataAsync(cancellationToken);

        await foreach ((string fileName, string data) in Readers)
        {
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
                    case EventOperation.ScalePartitions:
                        await ScaleNumberOfPartitions(operation, cancellationToken);
                        break;
                    case EventOperation.DeleteQueue:
                        await DeleteQueueAsync(operation, cancellationToken);
                        break;
                    case EventOperation.ClearQueue:
                        await ClearQueueAsync(operation, cancellationToken);
                        break;
                    default:
                        _logger.LogError($"Unknown operation found in the log file: {fileName}");
                        break;
                }
            }
        }
    }

    private async Task ScaleNumberOfPartitions(string[] operation, CancellationToken cancellationToken)
    {
        if (!Int32.TryParse(operation[2], out int newNumberOfPartitions) || newNumberOfPartitions <= 0)
        {
            _logger.LogError($"Cannot Parse the integer number of partitions = {operation[2]}, the operation will be ignored");
            return;
        }
        await _eventBus.ScaleNumberOfPartitions(operation[1], newNumberOfPartitions, cancellationToken, logEvent: false);
    }

    private async Task DeleteQueueAsync(string[] operation, CancellationToken cancellationToken)
    {
        await _eventBus.DeleteQueueAsync(operation[1], cancellationToken, logEvent: false);
    }

    private async Task CreateQueueAsync(string[] operation, CancellationToken cancellationToken)
    {
        if (!Int32.TryParse(operation[3], out int ackTimeout) || ackTimeout <= 0)
        {
            _logger.LogError($"Cannot Parse the integer ack timeout {operation[3]}"
                       + $"the queue, will be created with a default timeout of {DefaultAckTimeout} min");
            ackTimeout = DefaultAckTimeout;
        }
        if (!Int32.TryParse(operation[2], out int numberOfPartitions) || numberOfPartitions <= 0)
        {
            _logger.LogError($"Cannot Parse the integer number of partitions = {operation[2]}"
                       + $"the queue, will be created with a default number of partitions = 1");
            numberOfPartitions = 1;
        }
        await _eventBus.CreateQueueAsync(operation[1], ackTimeout, cancellationToken, logEvent: false, numberOfPartitions);
    }

    private async Task AckEventAsync(string[] operation, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(operation[2], out Guid eventId))
        {
            await _eventBus.AckAsync(operation[1], eventId, cancellationToken, logEvent: false);
        }
        _logger.LogError($"Cannot Parse the Guid eventId {operation[2]}, the event will not be acked");
    }

    private async Task PollEventAsync(string[] operation, CancellationToken cancellationToken)
    {
        await _eventBus.PollAsync(operation[1], cancellationToken, logEvent: false);
    }

    private async Task PushEventAsync(string[] operation, CancellationToken cancellationToken)
    {
        await _eventBus.PushEventAsync(operation[1], new Event().Deserialize(operation[2]), cancellationToken, logEvent: false);
    }

    private async Task ClearQueueAsync(string[] operation, CancellationToken cancellationToken)
    {
        await _eventBus.Clear(operation[1], cancellationToken, logEvent: false);
    }
}