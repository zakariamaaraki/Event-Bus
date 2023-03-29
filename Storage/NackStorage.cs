using Service_bus.Models;
using Service_bus.Models.LinkedDictionary;

namespace Service_bus.Storage;

public class NackStorage<T> : INackStorage<T> where T : AbstractEvent
{
    public int AckTimeout { get; }

    public NackStorage(int ackTimeout)
    {
        AckTimeout = ackTimeout;
    }

    private readonly LinkedDictionary<Guid, (T, DateTimeOffset)> _nackEvents = new();

    public List<(Guid, (T, DateTimeOffset))> GetAndRemoveTimedOutEvents(DateTimeOffset dateTimeOffset)
    {
        return _nackEvents.RemoveBasedOnCondition(tuple => dateTimeOffset.CompareTo(tuple.Item2.AddMinutes(AckTimeout)) >= 0);
    }

    public int Count()
    {
        return _nackEvents.Count();
    }

    public bool RemoveEvent(Guid eventId)
    {
        return _nackEvents.Remove(eventId);
    }

    public void AddEvent(Guid eventId, T newEvent)
    {
        _nackEvents.Put(eventId, (newEvent, DateTimeOffset.Now));
    }
}