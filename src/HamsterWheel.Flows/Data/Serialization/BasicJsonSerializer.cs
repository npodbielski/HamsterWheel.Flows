using System.Text.Json;

namespace HamsterWheel.Flows.Data.Serialization;

public class BasicJsonSerializer : ISerializer
{
    public string Serialize<T>(T obj) => JsonSerializer.Serialize(obj);
}