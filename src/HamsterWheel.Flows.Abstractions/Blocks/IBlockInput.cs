namespace HamsterWheel.Flows.Blocks;

public interface IBlockInput<out TInput>
{
    TInput Inputs { get; }
}