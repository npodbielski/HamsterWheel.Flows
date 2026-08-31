namespace HamsterWheel.Flows.IO;

public interface IComplexInputSource<T> : IInputSourceEnumerator<T>
{
    ITaskSource<T> EntireInput { get; }
}