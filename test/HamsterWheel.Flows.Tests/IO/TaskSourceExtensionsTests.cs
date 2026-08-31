using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Tests.IO;

public class TaskSourceExtensionsTests
{
    [Fact]
    public async Task WhenSourceBlockHasSingleResult_SetSourceStoresConvertedSingleValue()
    {
        //arrange
        var source = new TestExtBlock();
        source.Inputs.Const = "x";
        source.PushOutput("x");
        var target = new TaskSource<string>();

        //act
        target.SetSource(source, s => s + "!");

        //assert
        target.IsSingle.Should().BeTrue();
        (await target.GetSingle()).Should().Be("x!");
    }

    [Fact]
    public async Task WhenSourceBlockHasMultiResult_SetSourceStoresConvertedMultiValues()
    {
        //arrange
        var source = new TestExtBlock();
        var target = new TaskSource<string>();
        target.SetSource(source, s => s + "!");
        var resultTask = target.GetMulti().ToListAsync();

        //act
        source.PushOutput("a");
        source.Result.Finish();
        source.Complete();
        var result = await resultTask;

        //assert
        target.IsMulti.Should().BeTrue();
        result.Should().BeEquivalentTo(["a!"]);
    }

    [Fact]
    public async Task WhenTaskMultiSourceWithConversion_GetMultiYieldsConvertedItems()
    {
        //arrange
        var target = new TaskSource<string>();

        //act
        target.SetSource(Task.FromResult(new[] { "a", "b" }), s => s.ToUpper());

        //assert
        target.IsMulti.Should().BeTrue();
        (await target.GetMulti().ToListAsync()).Should().BeEquivalentTo(["A", "B"]);
    }

    [Fact]
    public async Task WhenTaskMultiSourceToObject_GetMultiYieldsItemsWithoutConversion()
    {
        //arrange
        var target = new TaskSource<object>();

        //act
        target.SetSource(Task.FromResult(new object[] { "a", 1 }), o => o);

        //assert
        (await target.GetMulti().ToListAsync()).Should().BeEquivalentTo(new object[] { "a", 1 });
    }

    private class TestExtBlock : PipelineBlock<string, string, SingleInputTaskSource<string>>
    {
        public override SingleInputTaskSource<string> Inputs { get; } = new();
        public override Task<string> RunForInput(string input, CancellationToken token) => Task.FromResult(input);
        public void Complete() => SetResult();
        public void PushOutput(string output) => ((BlockResult<string>)Result).PushOutput(output);
    }
}
