namespace HamsterWheel.Flows.IO;

public interface IInputSourceEnumerator<out T> : IInputInfo
{
    IAsyncEnumerable<T> Get(CancellationToken cancellationToken = default);
}