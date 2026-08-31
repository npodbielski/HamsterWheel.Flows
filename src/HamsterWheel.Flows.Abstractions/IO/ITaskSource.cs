using HamsterWheel.Flows.Blocks;

namespace HamsterWheel.Flows.IO;

public interface ITaskSource<T> : ITaskSource
{
    T Const { get; set; }
    void SetSource<T1>(IPipelineBlock<T1> addApiToken) where T1 : class, T;
    void SetSource<T1>(Task<T1> multiSource) where T1 : IEnumerable<T>;
    void SetSource(IPipelineBlock<T> sourceBlock);
    void SetSource(Task<T> task);
    IAsyncEnumerable<T> GetMulti();
    Task<T> GetSingle();
    void SetSource(IAsyncEnumerable<T> source);
}

public interface ITaskSource
{
    bool IsSet { get; }
    bool IsSingle { get; }
    bool IsMulti { get; }
}