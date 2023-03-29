namespace Service_bus.Exceptions;

public class InvalidEventException : CustomException
{
    public InvalidEventException(string message) : base(message, null, System.Net.HttpStatusCode.BadRequest) { }
}