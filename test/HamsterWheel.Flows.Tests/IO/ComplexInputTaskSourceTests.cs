using FluentAssertions;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Tests.IO;

public class ComplexInputTaskSourceTests
{
    [Fact]
    public async Task WhenEntireInputConst_GetYieldsOnlyEntireInput()
    {
        //arrange
        var sut = new TestComplexSource();
        sut.EntireInput.Const = "entire";

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().BeEquivalentTo(["entire"]);
        sut.GetImplInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task WhenEntireInputSingleTask_GetYieldsOnlyEntireInput()
    {
        //arrange
        var sut = new TestComplexSource();
        sut.EntireInput.SetSource(Task.FromResult("entire"));

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().BeEquivalentTo(["entire"]);
        sut.GetImplInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task WhenEntireInputMulti_GetYieldsEntireInputThenImplItems()
    {
        //arrange
        var sut = new TestComplexSource
        {
            ImplItems = Items("impl-1")
        };
        sut.EntireInput.SetSource(EntireItems());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().BeEquivalentTo(["entire-1", "entire-2", "impl-1"]);
        sut.GetImplInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEntireInputNotSet_GetYieldsImplItems()
    {
        //arrange
        var sut = new TestComplexSource
        {
            ImplItems = Items("impl-1", "impl-2")
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().BeEquivalentTo(["impl-1", "impl-2"]);
        sut.GetImplInvoked.Should().BeTrue();
    }

    private class TestComplexSource : ComplexInputTaskSource<string>
    {
        public bool GetImplInvoked { get; private set; }
        public IAsyncEnumerable<string> ImplItems { get; set; } = Items("unused");

        public override bool AllSingle => EntireInput.IsSingle || !EntireInput.IsSet;

        public override IAsyncEnumerable<string> GetImpl(CancellationToken cancellationToken = default)
        {
            GetImplInvoked = true;
            return ImplItems;
        }
    }

    private static async IAsyncEnumerable<string> EntireItems()
    {
        yield return "entire-1";
        await Task.Yield();
        yield return "entire-2";
    }

    private static async IAsyncEnumerable<string> Items(params string[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}
