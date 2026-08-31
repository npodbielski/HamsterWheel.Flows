using FluentAssertions;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Logic;

public class TakeAllBlockTests
{
    [Fact]
    public async Task WhenMultiInputs_TakeAllYieldsSingleArrayWithAllItems()
    {
        //arrange
        var block = new TakeAllBlock();
        var pipeline = block.InitBlock();
        block.Inputs.SetSource(Items());

        //act
        await pipeline.Run().WaitSeconds(10);

        //assert
        //multi inputs make the block multi-output, so the value is read via GetLastOutput, not SingleValue
        var result = ((BlockResult<object[][]>)block.Result).GetLastOutput();
        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Fact]
    public async Task WhenConstInput_TakeAllYieldsArrayWithInputItem()
    {
        //arrange
        var block = new TakeAllBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Const = new object[] { 1, 2, 3 };

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(10);

        //assert
        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(new object[] { new object[] { 1, 2, 3 } });
    }

    [Fact]
    public async Task WhenInputTransformerSet_TakeAllUsesTransformedInput()
    {
        //arrange
        var block = new TakeAllBlock();
        var pipeline = block.InitBlock();
        block.Inputs.SetSource(Items());
        block.InputTransformer = (logger, input) => input;

        //act
        await pipeline.Run().WaitSeconds(10);

        //assert
        var result = ((BlockResult<object[][]>)block.Result).GetLastOutput();
        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(new object[] { 1, 2, 3 });
    }

    private static async IAsyncEnumerable<object> Items()
    {
        yield return 1;
        await Task.Yield();
        yield return 2;
        yield return 3;
    }
}
