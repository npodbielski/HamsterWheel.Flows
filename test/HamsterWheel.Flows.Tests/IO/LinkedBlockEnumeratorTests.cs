using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Tests.IO;

public class LinkedBlockEnumeratorTests
{
    [Fact]
    public async Task WhenItemsPushedAndOwnerCompletes_GetYieldsPushedItems()
    {
        //arrange
        var owner = new TestLinkedBlock();
        var sut = new LinkedBlockEnumerator<string>(owner);
        sut.Push("a");
        sut.Push("b");
        var resultTask = sut.Get().ToListAsync();

        //act
        owner.Complete();
        var result = await resultTask;

        //assert
        result.Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public async Task WhenSingleItemPushedAndOwnerCompletes_GetYieldsThatItem()
    {
        //arrange
        var owner = new TestLinkedBlock();
        var sut = new LinkedBlockEnumerator<string>(owner);
        sut.Push("a");
        var resultTask = sut.Get().ToListAsync();

        //act
        owner.Complete();
        var result = await resultTask;

        //assert
        result.Should().BeEquivalentTo(["a"]);
    }

    [Fact]
    public async Task WhenOwnerCompletesBeforeAnyItem_GetYieldsNothing()
    {
        //arrange
        var owner = new TestLinkedBlock();
        var sut = new LinkedBlockEnumerator<string>(owner);
        sut.Finish();
        var resultTask = sut.Get().ToListAsync();

        //act
        owner.Complete();
        var result = await resultTask;

        //assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTokenCancelled_GetYieldsNothing()
    {
        //arrange
        var owner = new TestLinkedBlock();
        var sut = new LinkedBlockEnumerator<string>(owner);
        sut.Finish();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        //act
        var result = await sut.Get(cts.Token).ToListAsync();

        //assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetCalledAfterEnumerationExhausted_GetYieldsNothing()
    {
        //arrange
        var owner = new TestLinkedBlock();
        var sut = new LinkedBlockEnumerator<string>(owner);
        sut.Push("a");
        var first = sut.Get().ToListAsync();
        owner.Complete();
        await first;

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().BeEmpty();
    }

    private class TestLinkedBlock : PipelineBlock<string, string, SingleInputTaskSource<string>>
    {
        public override SingleInputTaskSource<string> Inputs { get; } = new();
        public override Task<string> RunForInput(string input, CancellationToken token) => Task.FromResult(input);
        public void Complete() => SetResult();
    }
}
