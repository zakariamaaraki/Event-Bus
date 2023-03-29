namespace Service_bus.Storage;

public interface INackStorage<T>
{
    int AckTimeout { get; }

    List<(Guid, (T, DateTimeOffset))> GetAndRemoveTimedOutEvents(DateTimeOffset dateTimeOffset);

    int Count();

    bool RemoveEvent(Guid eventId);

    void AddEvent(Guid eventId, T newEvent);
}