using System.Net;

namespace Service_bus.Exceptions;

/// <summary>
/// A custom exception used as base class for other custom exceptions.
/// This class contains two other properties (ErrorMessages and the StatusCode) in addition to the message.
/// </summary>
public class CustomException : Exception
{
    public List<string>? ErrorMessages { get; }

    public HttpStatusCode StatusCode { get; }

    public CustomException(string message, List<string>? errors = default, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors;
        StatusCode = statusCode;
    }
}