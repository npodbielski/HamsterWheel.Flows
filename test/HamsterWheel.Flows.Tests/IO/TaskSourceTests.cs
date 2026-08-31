using System.Runtime.CompilerServices;
using FluentAssertions;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.IO;

public class TaskSourceTests
{
    [Fact]
    public void WhenConstIsSet_ThenGetSingleReturnsConst()
    {
        //arrange
        var source = new TaskSource<string>();
        source.Const = "hello";

        //act
        var result = source.GetSingle().Result;

        //assert
        result.Should().Be("hello");
    }

    [Fact]
    public void WhenTaskIsSet_ThenGetSingleReturnsTaskResult()
    {
        //arrange
        var source = new TaskSource<int>();
        source.SetSource(Task.FromResult(42));

        //act
        var result = source.GetSingle().Result;

        //assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task WhenMultiSourceIsSet_ThenGetMultiYieldsAllItems()
    {
        //arrange
        var source = new TaskSource<int>();
        source.SetSource(AsyncRange(1, 5));

        //act
        var items = new List<int>();
        await foreach (var item in source.GetMulti())
        {
            items.Add(item);
        }

        //assert
        items.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void WhenNothingIsSet_ThenIsSetIsFalse()
    {
        //arrange
        var source = new TaskSource<string>();

        //act
        // (property access)

        //assert
        source.IsSet.Should().BeFalse();
        source.IsSingle.Should().BeFalse();
        source.IsMulti.Should().BeFalse();
    }

    [Fact]
    public void WhenConstIsSet_ThenIsSingleIsTrue()
    {
        //arrange
        var source = new TaskSource<string>();
        source.Const = "value";

        //act
        // (property access)

        //assert
        source.IsSingle.Should().BeTrue();
        source.IsSet.Should().BeTrue();
    }

    [Fact]
    public void WhenMultiSourceIsSet_ThenIsMultiIsTrue()
    {
        //arrange
        var source = new TaskSource<int>();
        source.SetSource(AsyncRange(1, 3));

        //act
        // (property access)

        //assert
        source.IsMulti.Should().BeTrue();
    }

    [Fact]
    public async Task WhenConstIsSet_ThenGetMultiYieldsConstValue()
    {
        //arrange
        var source = new TaskSource<string>();
        source.Const = "single";

        //act
        var items = new List<string>();
        await foreach (var item in source.GetMulti())
        {
            items.Add(item);
        }

        //assert
        items.Should().BeEquivalentTo(["single"]);
    }

    [Fact]
    public void WhenConstIsSetAndGetSingleWithoutConst_ThenThrows()
    {
        //arrange
        var source = new TaskSource<string>();

        //act
        var action = () => source.Const;

        //assert
        action.Should().Throw<Exception>();
    }

    [Fact]
    public async Task WhenSetSourceWithSingleBlock_ThenGetSingleReturnsBlockOutput()
    {
        //arrange
        //TaskSource<object> forces the generic SetSource<T1>(IPipelineBlock<T1>) overload (T1=string : object)
        var sourceBlock = new TestSourceBlock();
        sourceBlock.Inputs.Const = "from-block";
        sourceBlock.PushOutput("from-block");
        var source = new TaskSource<object>();

        //act
        source.SetSource(sourceBlock);

        //assert
        source.IsSingle.Should().BeTrue();
        (await source.GetSingle()).Should().Be("from-block");
    }

    [Fact]
    public async Task WhenSetSourceWithMultiBlock_ThenGetMultiYieldsBlockOutputs()
    {
        //arrange
        var sourceBlock = new TestSourceBlock();
        var source = new TaskSource<object>();
        source.SetSource(sourceBlock);
        var resultTask = source.GetMulti().ToListAsync();

        //act
        sourceBlock.PushOutput("a");
        sourceBlock.Result.Finish();
        sourceBlock.Complete();
        var result = await resultTask;

        //assert
        source.IsMulti.Should().BeTrue();
        result.Should().BeEquivalentTo(new object[] { "a" });
    }

    private class TestSourceBlock : PipelineBlock<string, string, SingleInputTaskSource<string>>
    {
        public override SingleInputTaskSource<string> Inputs { get; } = new();
        public override Task<string> RunForInput(string input, CancellationToken token) => Task.FromResult(input);
        public void Complete() => SetResult();
        public void PushOutput(string output) => ((BlockResult<string>)Result).PushOutput(output);
    }

    private static async IAsyncEnumerable<int> AsyncRange(int from, int to, [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (var i = from; i <= to; i++)
        {
            yield return i;
            await Task.Delay(1, ct);
        }
    }
}
