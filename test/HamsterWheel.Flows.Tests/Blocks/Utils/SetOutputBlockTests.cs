using FluentAssertions;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Dummies;
using HamsterWheel.Flows.Tests.Utils;
using static HamsterWheel.Data.Mapper.DataMapper;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class SetOutputBlockTests
{
    //public SetOutputBlockTests() => JsonSerializerInstance.Instance = new JsonSerializerService();

    [Fact]
    public async Task WhenCorrectPathAndValue_ThenSetsValue()
    {
        //arrange
        var expected = new
        {
            Id = Guid.NewGuid(),
            Name = "Nested"
        };
        var firstInstallResult = new DummyEntity("DummyName");
        var sut = new SetOutputBlock();
        var pipeline = sut.InitBlock(firstInstallResult);
        sut.Inputs.PropertyPath.Const = nameof(DummyEntity.Nested);
        sut.Inputs.Value.Const = new NestedDummyEntity
        {
            Id = expected.Id,
            Name = expected.Name
        };

        await pipeline.Run().WaitSeconds(1);
        var actual = firstInstallResult.Nested;

        //assert
        sut.Completion.IsCompleted.Should().BeTrue();
        sut.Completion.IsFaulted.Should().BeFalse();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenCorrectPathAndValueButDifferentType_ThenSetsValue()
    {
        //arrange 
        var expected = new
        {
            Id = Guid.NewGuid(),
            Name = "Nested"
        };
        var dummy = new DummyEntity("DummyName");
        var sut = new SetOutputBlock();
        var pipeline = sut.InitBlock(dummy);
        sut.Inputs.PropertyPath.Const = nameof(DummyEntity.Nested);
        sut.Inputs.Value.Const = new
        {
            expected.Id,
            expected.Name,
            ExtraProp = 123
        };

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = dummy.Nested;

        //assert
        sut.Completion.IsCompleted.Should().BeTrue();
        sut.Completion.IsFaulted.Should().BeFalse();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenHaveTransformation_ThenSetsValue()
    {
        //arrange 
        var expected = new
        {
            Id = Guid.NewGuid(),
            Name = "Nested",
            Number = 19
        };
        var dummy = new DummyEntity("DummyName");
        var sut = new SetOutputBlock();
        var pipeline = sut.InitBlock(dummy);
        sut.Inputs.PropertyPath.Const = nameof(DummyEntity.Nested);
        sut.Inputs.Value.Const = new
        {
            Id = Guid.NewGuid(),
            Name = "Nested",
            Number = 19
        };
        sut.Inputs.Map.Const = [(AnyProperty, AnyProperty), (nameof(expected.Number), nameof(DummyEntity.Nested.Int))];

        //act
        await pipeline.Run().WaitSeconds(1);

        //assert
        dummy.Nested.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenHaveTransformationAndDifferentTypeOfProperties_ThenSetsValue()
    {
        //arrange 
        var expected = new
        {
            Id = Guid.NewGuid(),
            Name = "Nested",
            Number = 19
        };
        var dummy = new DummyEntity("DummyName");
        var sut = new SetOutputBlock();
        var pipeline = sut.InitBlock(dummy);
        sut.Inputs.PropertyPath.Const = nameof(DummyEntity.Nested);
        sut.Inputs.Value.Const = new
        {
            Id = Guid.NewGuid(),
            Name = "Nested",
            Number = "19"
        };
        sut.Inputs.Map.Const = [(AnyProperty, AnyProperty), (nameof(expected.Number), nameof(DummyEntity.Int))];

        //act
        await pipeline.Run().WaitSeconds(1);

        //assert
        dummy.Nested.Should().NotBeNull();
    }
}