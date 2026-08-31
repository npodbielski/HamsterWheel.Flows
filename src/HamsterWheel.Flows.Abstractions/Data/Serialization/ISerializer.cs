namespace HamsterWheel.Flows.Data.Serialization;

public interface ISerializer
{
    string Serialize<T>(T obj);
}