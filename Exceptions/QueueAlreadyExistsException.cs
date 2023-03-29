namespace Service_bus.Exceptions;

public class QueueAlreadyExistsException : CustomException
{
    public QueueAlreadyExistsException(string message) : base(message, null, System.Net.HttpStatusCode.Conflict) { }
}