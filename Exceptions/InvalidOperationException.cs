
namespace Service_bus.Exceptions;

public class ServiceBusInvalidOperationException : CustomException
{
    public ServiceBusInvalidOperationException(string message) : base(message, null, System.Net.HttpStatusCode.BadRequest) { }
}