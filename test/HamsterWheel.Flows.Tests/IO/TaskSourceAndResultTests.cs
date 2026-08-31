using System.Runtime.CompilerServices;
using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.IO;

public class BlockResultTests
{


    [Fact]
    public void WhenGetLastOutput_ThenReturnsLast()
    {
        //arrange
        var owner = new TestSingleBlock();
        var result = new BlockResult<string>(owner);

        //act
        result.PushOutput("first");
        result.PushOutput("second");
        var last = result.GetLastOutput();

        //assert
        last.Should().Be("second");
    }



    private class TestSingleBlock : PipelineBlock<string, string, SingleInputTaskSource<string>>
    {
        public override SingleInputTaskSource<string> Inputs { get; } = new();
        public override Task<string> RunForInput(string input, CancellationToken token) => Task.FromResult(input);
    }

    private class TestMultiBlock : MultiplierPipelineBlock<object, object, SingleInputTaskSource<object>>
    {
        public override SingleInputTaskSource<object> Inputs { get; } = new();
        public override Task RunForInput(object input, Action<object> pushOutput, CancellationToken token)
        {
            pushOutput(input);
            return Task.CompletedTask;
        }
    }
}

public class TaskSourceDirectTests
{
    [Fact]
    public void WhenSetSourceWithTask_ThenSourceIsSet()
    {
        //arrange
        var taskSource = new TaskSource<string>();

        //act
        taskSource.SetSource(Task.FromResult("hello"));

        //assert
        taskSource.IsSingle.Should().BeTrue();
        taskSource.IsSet.Should().BeTrue();
    }

    [Fact]
    public async Task WhenSetConst_ThenSingleValueWorks()
    {
        //arrange
        var taskSource = new TaskSource<int>();
        taskSource.Const = 42;

        //act
        var result = await taskSource.GetSingle();

        //assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task WhenSetSourceWithMulti_ThenAsEnumerableWorks()
    {
        //arrange
        var taskSource = new TaskSource<string>();
        taskSource.SetSource(Task.FromResult(new[] { "a", "b", "c" }));

        //act
        var result = await taskSource.GetMulti().ToListAsync().AsTask().WaitSeconds(5);

        //assert
        result.Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [Fact]
    public void WhenNotSet_ThenIsSetIsFalse()
    {
        //arrange
        var taskSource = new TaskSource<string>();

        //act
        // (property access)

        //assert
        taskSource.IsSet.Should().BeFalse();
    }
}
