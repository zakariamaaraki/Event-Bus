namespace Service_bus.Storage;

/// <summary>
/// Used to store events waiting for acks.
/// </summary>
/// <typeparam name="T">The type of the event.</typeparam>
public interface INackStorage<T>
{
    int AckTimeout { get; }

    /// <summary>
    /// Get and Remove timedout events.
    /// </summary>
    /// <param name="dateTimeOffset">The dateTimeOffset when the event was added to the storage.</param>
    /// <returns>A List<(Guid, (T, DateTimeOffset))>, represents the id, the events 
    /// and the dateTimeOffset when the events was added to the storage.</returns>
    List<(Guid, (T, DateTimeOffset))> GetAndRemoveTimedOutEvents(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// Number of stored events.
    /// </summary>
    /// <returns>An Integer representing the number of stored events.</returns>
    int Count();

    /// <summary>
    /// Remove the event from the storage.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>True if the events was found, false otherwise.</returns>
    bool RemoveEvent(Guid eventId);

    /// <summary>
    /// Add an event to the storage.
    /// </summary>
    /// <param name="eventId">The events id.</param>
    /// <param name="newEvent">The event.</param>
    void AddEvent(Guid eventId, T newEvent);
}