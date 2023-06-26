using Service_bus.Exceptions;
using Service_bus.Models;
using Service_bus.Models.LinkedDictionary;

namespace Service_bus.Storage;

/// <summary>
/// Used to store events waiting for acks.
/// </summary>
/// <typeparam name="T">The type of the event.</typeparam>
public class NackStorage<T> : INackStorage<T> where T : AbstractEvent
{
    public int AckTimeout { get; }

    public NackStorage(int ackTimeout)
    {
        AckTimeout = ackTimeout;
    }

    private readonly LinkedDictionary<Guid, (T, DateTimeOffset)> _nackEvents = new();

    /// <summary>
    /// Get and Remove timedout events.
    /// </summary>
    /// <param name="dateTimeOffset">The dateTimeOffset when the event was added to the storage.</param>
    /// <returns>A List<(Guid, (T, DateTimeOffset))>, represents the id, the events 
    /// and the dateTimeOffset when the events was added to the storage.</returns>
    public List<(Guid, (T, DateTimeOffset))> GetAndRemoveTimedOutEvents(DateTimeOffset dateTimeOffset)
    {
        return _nackEvents.RemoveBasedOnCondition(tuple => dateTimeOffset.CompareTo(tuple.Item2.AddMinutes(AckTimeout)) >= 0);
    }

    /// <summary>
    /// Number of stored events.
    /// </summary>
    /// <returns>An Integer representing the number of stored events.</returns>
    public int Count()
    {
        return _nackEvents.Count();
    }

    /// <summary>
    /// Remove the event from the storage.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>True if the events was found, false otherwise.</returns>
    public bool RemoveEvent(Guid eventId)
    {
        return _nackEvents.Remove(eventId);
    }

    /// <summary>
    /// Add an event to the storage.
    /// </summary>
    /// <param name="eventId">The events id.</param>
    /// <param name="newEvent">The event.</param>
    public void AddEvent(Guid eventId, T newEvent)
    {
        _nackEvents.Put(eventId, (newEvent, DateTimeOffset.Now));
    }

    /// <summary>
    /// Get an event from the storage based on its key.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>The event.</returns>
    public T GetEvent(Guid eventId)
    {
        try
        {
            return _nackEvents.Get(eventId).Item1;
        }
        catch (KeyNotFoundException)
        {
            throw new EventNotFoundException("The event was nout found in the nack storage");
        }
    }

    /// <summary>
    /// Check if an event exists.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>The event.</returns>
    public bool ContainsEvent(Guid eventId)
    {
        return _nackEvents.ContainsKey(eventId);
    }
}