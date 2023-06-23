
namespace Service_bus.Exceptions;

public class InvalidOperationException : CustomException
{
    public InvalidOperationException(string message) : base(message, null, System.Net.HttpStatusCode.BadRequest) { }
}