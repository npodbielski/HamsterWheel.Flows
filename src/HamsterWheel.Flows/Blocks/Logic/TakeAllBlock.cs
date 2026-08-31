using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Logic;

public class TakeAllBlock : GroupingBlock<object, object[], SingleInputTaskSource<object>>
{
    public override Task<IEnumerable<object[]>> RunForInput(IEnumerable<object> input) =>
        Task.FromResult<IEnumerable<object[]>>([input.ToArray()]);
}