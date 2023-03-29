namespace Service_bus.Exceptions;

public class InvalidArgumentException : CustomException
{
    public InvalidArgumentException(string message) : base(message, null, System.Net.HttpStatusCode.BadRequest) { }
}