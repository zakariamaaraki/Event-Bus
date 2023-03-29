namespace Service_bus.Exceptions;

[Serializable]
public class QueueNotFoundException : CustomException
{
    public QueueNotFoundException(string message) : base(message, null, System.Net.HttpStatusCode.NotFound) { }
}