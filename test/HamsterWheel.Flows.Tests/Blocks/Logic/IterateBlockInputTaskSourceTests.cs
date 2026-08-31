using FluentAssertions;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Tests.Blocks.Logic;

public class IterateBlockInputTaskSourceTests
{
    [Fact]
    public void WhenSourceSet_AllSingleIsAlwaysFalse()
    {
        //arrange
        var sut = new IterateBlockInputTaskSource();
        sut.SetSource(Task.FromResult(new[] { 1, 2, 3 }));

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeFalse();
    }

    [Fact]
    public async Task WhenSourceSetWithArray_GetYieldsAllItemsAsOneEnumerable()
    {
        //arrange
        var sut = new IterateBlockInputTaskSource();
        sut.SetSource(Task.FromResult(new[] { 1, 2, 3 }));

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Fact]
    public async Task WhenSourceSetWithSingleNestedArray_GetFlattensNestedItems()
    {
        //arrange
        //a single item that is itself a non-empty enumerable is flattened to its elements
        var sut = new IterateBlockInputTaskSource();
        sut.SetSource(Task.FromResult(new object[] { new[] { 1, 2, 3 } }));

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(new object[] { 1, 2, 3 });
    }
}
