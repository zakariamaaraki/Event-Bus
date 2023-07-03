using Service_bus.Volumes;

namespace Service_bus.Services;

/// <summary>
/// A worker which handler timeout checks, log compaction and other operations.
/// </summary>
public class EventProcessor : IHostedService
{
    private readonly ILogger<EventProcessor> _logger;
    private readonly IEventsLoader _eventsLoader;
    private readonly IEventBus _eventBus;

    private const int TimeoutCheckerFrequency = 1; // 1 minute
    private const int LogFilesCompactionFrequency = 30; // 30 minutes

    public EventProcessor(ILogger<EventProcessor> logger, IEventsLoader eventsLoader, IEventBus eventBus)
    {
        _logger = logger;
        _eventsLoader = eventsLoader;
        _eventBus = eventBus;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TimeoutCheckerJob(cancellationToken);
        LogFilesCompactionJob();
        return _eventsLoader.Load(cancellationToken);
    }

    private void LogFilesCompactionJob()
    {
        var startTimeSpan = TimeSpan.Zero;
        var periodTimeSpan = TimeSpan.FromMinutes(LogFilesCompactionFrequency);

        var timer = new System.Threading.Timer((e) =>
        {
            _logger.LogInformation("Running log files compaction job");
            // TODO: compact log files
        }, null, startTimeSpan, periodTimeSpan);
    }

    private void TimeoutCheckerJob(CancellationToken cancellationToken)
    {
        var startTimeSpan = TimeSpan.FromMinutes(TimeoutCheckerFrequency);
        var periodTimeSpan = TimeSpan.FromMinutes(TimeoutCheckerFrequency);

        var timer = new System.Threading.Timer(async (e) =>
        {
            _logger.LogInformation("Running timeout checker job");
            int numberOfTimeouts = await _eventBus.TriggerTimeoutChecksAsync(cancellationToken);
            _logger.LogInformation($"{numberOfTimeouts} event(s) requeued by the Timeout checker job");
        }, null, startTimeSpan, periodTimeSpan);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}