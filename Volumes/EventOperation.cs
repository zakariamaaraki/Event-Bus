using System.Text.Json.Serialization;

namespace Service_bus.Volumes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventOperation
{
    Push,
    Poll,
    Ack,
    CreateQueue,
    ScalePartitions,
    DeleteQueue
}