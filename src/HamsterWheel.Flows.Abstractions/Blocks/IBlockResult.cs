namespace HamsterWheel.Flows.Blocks;

public interface IBlockResult<TOutput>
{
    bool IsSingle { get; }
    Task<TOutput> SingleValue { get; }
    IAsyncEnumerable<TOutput> AsEnumerable();
    void Finish();
}