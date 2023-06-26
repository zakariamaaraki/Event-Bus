
namespace Service_bus.Exceptions;

public class EventNotFoundException : CustomException
{
    public EventNotFoundException(string message) : base(message, null, System.Net.HttpStatusCode.BadRequest) { }
}