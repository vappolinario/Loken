namespace Loken.Core;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoStatus
{
    [JsonPropertyName("todo")]
    Todo,
    [JsonPropertyName("doing")]
    Doing,
    [JsonPropertyName("done")]
    Done
}
