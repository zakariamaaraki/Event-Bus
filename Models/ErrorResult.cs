namespace Service_bus.Models;

/// <summary>
/// This class is used as an output to the caller in case of a custom exception occurs.
/// </summary>
public class ErrorResult
{
    public List<string> Messages { get; set; } = new();

    public string? Source { get; set; }
    public string? Exception { get; set; }
    public string? ErrorId { get; set; }
    public string? SupportMessage { get; set; }
    public int StatusCode { get; set; }
}