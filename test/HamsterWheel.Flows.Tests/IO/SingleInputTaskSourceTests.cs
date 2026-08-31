using FluentAssertions;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Tests.IO;

public class SingleInputTaskSourceTests
{
    [Fact]
    public async Task WhenConstIsSet_ThenGetYieldsSingleValue()
    {
        //arrange
        var source = new SingleInputTaskSource<string>();
        source.Const = "hello";

        //act
        var items = new List<string>();
        await foreach (var item in source.Get())
        {
            items.Add(item);
        }

        //assert
        items.Should().BeEquivalentTo(["hello"]);
    }

    [Fact]
    public async Task WhenTaskSourceIsSet_ThenGetYieldsTaskResult()
    {
        //arrange
        var source = new SingleInputTaskSource<int>();
        source.SetSource(Task.FromResult(99));

        //act
        var items = new List<int>();
        await foreach (var item in source.Get())
        {
            items.Add(item);
        }

        //assert
        items.Should().BeEquivalentTo([99]);
    }

    [Fact]
    public async Task WhenMultiSourceIsSet_ThenGetYieldsAllItems()
    {
        //arrange
        var source = new SingleInputTaskSource<int>();
        source.SetSource(AsyncItems(1, 2, 3));

        //act
        var items = new List<int>();
        await foreach (var item in source.Get())
        {
            items.Add(item);
        }

        //assert
        items.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void WhenConstIsSet_ThenAllSingleIsTrue()
    {
        //arrange
        var source = new SingleInputTaskSource<string>();
        source.Const = "value";

        //act
        // (property access)

        //assert
        source.AllSingle.Should().BeTrue();
    }

    [Fact]
    public void WhenNothingIsSet_ThenAllSingleIsFalse()
    {
        //arrange
        var source = new SingleInputTaskSource<string>();

        //act
        // (property access)

        //assert
        source.AllSingle.Should().BeFalse();
    }

    private static async IAsyncEnumerable<int> AsyncItems(params int[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Delay(1);
        }
    }
}
