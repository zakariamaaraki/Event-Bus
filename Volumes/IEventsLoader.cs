using Service_bus.Models;

namespace Service_bus.Volumes;

public interface IEventsLoader
{
    Task Load(CancellationToken cancellationToken);
}