using FluentAssertions;
using HamsterWheel.Flows.Blocks;

namespace HamsterWheel.Flows.Tests.Blocks;

public class SubFlowTaskSourceTests
{
    [Fact]
    public async Task WhenAllConst_GetYieldsSingleInputWithCorrectValues()
    {
        //arrange
        var sut = new SubFlowTaskSource
        {
            FlowName = { Const = "my-flow" },
            Input = { Const = "my-input" }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].FlowName.Should().Be("my-flow");
        result[0].Input.Should().Be("my-input");
    }

    [Fact]
    public void WhenAllConst_AllSingleIsTrue()
    {
        //arrange
        var sut = new SubFlowTaskSource
        {
            FlowName = { Const = "my-flow" },
            Input = { Const = "my-input" }
        };

        //act
        // (property is read directly)

        //assert
        sut.AllSingle.Should().BeTrue();
    }

    [Fact]
    public void WhenSingleTaskSources_AllSingleIsTrue()
    {
        //arrange
        var sut = new SubFlowTaskSource();
        sut.FlowName.SetSource(Task.FromResult("my-flow"));
        sut.Input.SetSource(Task.FromResult<object?>("my-input"));

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeTrue();
    }

    [Fact]
    public async Task WhenMultiFlowNames_GetYieldsPerItem()
    {
        //arrange
        var sut = new SubFlowTaskSource();
        sut.FlowName.SetSource(FlowNames());
        sut.Input.SetSource(Inputs());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].FlowName.Should().Be("flow-a");
        result[0].Input.Should().Be(1);
        result[1].FlowName.Should().Be("flow-b");
        result[1].Input.Should().Be(2);
    }

    [Fact]
    public void WhenMultiFlowNames_AllSingleIsFalse()
    {
        //arrange
        var sut = new SubFlowTaskSource();
        sut.FlowName.SetSource(FlowNames());

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeFalse();
    }

    [Fact]
    public async Task WhenMultiFlowNamesAndInputUnset_GetYieldsNullInputs()
    {
        //arrange
        var sut = new SubFlowTaskSource();
        sut.FlowName.SetSource(FlowNames());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.Input.Should().BeNull());
    }

    private static async IAsyncEnumerable<string> FlowNames()
    {
        yield return "flow-a";
        await Task.Yield();
        yield return "flow-b";
    }

    private static async IAsyncEnumerable<object?> Inputs()
    {
        yield return 1;
        await Task.Yield();
        yield return 2;
    }
}
