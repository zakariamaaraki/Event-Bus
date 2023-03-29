namespace Service_bus.Exceptions;

public class NoEventFoundException : CustomException
{
    public NoEventFoundException(string message) : base(message, null, System.Net.HttpStatusCode.NotFound) { }
}