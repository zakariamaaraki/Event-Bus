namespace Service_bus.Exceptions;

/// <summary>
/// This exception is used when a given Event is Invalid
/// </summary>
public class InvalidEventException : CustomException
{
    public InvalidEventException(string message) : base(message, null, System.Net.HttpStatusCode.BadRequest) { }
}