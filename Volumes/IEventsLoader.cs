using Service_bus.Models;

namespace Service_bus.Volumes;

/// <summary>
/// Load all events from an external storage or log files.
/// </summary>
public interface IEventsLoader
{
    /// <summary>
    /// Load events from an external storage or log files.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task.</returns>
    Task Load(CancellationToken cancellationToken);
}