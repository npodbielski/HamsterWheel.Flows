using FluentAssertions;
using HamsterWheel.Data.Mapper;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Dummies;
using HamsterWheel.Flows.Tests.Utils;
using HamsterWheel.HLinq.Exceptions;
using static HamsterWheel.Data.Mapper.DataMapper;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class SetPropertyBlockTests
{
    [Fact]
    public async Task WhenCorrectPathAndValue_ThenSetsValue()
    {
        //arrange
        var expected = "5.0";
        var sut = new SetPropertyBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Object.Const = new MyClass { Version = "1.0" }!;
        sut.Inputs.PropertyPath.Const = "version";
        sut.Inputs.Value.Const = expected;

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = (MyClass)await sut.Result.SingleValue;

        //assert
        actual.Version.Should().Be(expected);
    }

    [Fact]
    public async Task WhenCorrectPathAndValueAndTransformation_ThenSetsValueUsingTransform()
    {
        //arrange
        var expected = new DummyEntity("DummyName")
        {
            Nested = new()
            {
                Name = "token-name",
                Id = Guid.NewGuid(),
            }
        };
        var sut = new SetPropertyBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Map.Const = [(AnyProperty, AnyProperty)];
        sut.Inputs.Object.Const = new DummyEntity("DummyName");
        sut.Inputs.PropertyPath.Const = nameof(DummyEntity.Nested);
        sut.Inputs.Value.Const = new
        {
            expected.Nested.Id,
            Data = new
            {
                expected.Nested.Name,
            }
        };

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = (DummyEntity)await sut.Result.SingleValue;

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenIncorrectPath_ThenThrows()
    {
        //arrange
        var expected = "5.0";
        var sut = new SetPropertyBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Object.Const = new MyClass { Version = "1.0" };
        sut.Inputs.PropertyPath.Const = "a";
        sut.Inputs.Value.Const = expected;
        var action = () => pipeline.Run().WaitSeconds(1);

        //act
        await action.Should().ThrowAsync<MissingPropertyException>();

        //assert
        sut.Completion.IsFaulted.Should().BeTrue();
    }

    [Fact]
    public async Task WhenIncorrectValue_ThenThrows()
    {
        //arrange
        var sut = new SetPropertyBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Object.Const = new SinglePropInput<DateTime> { Prop = DateTime.Now }!;
        sut.Inputs.PropertyPath.Const = nameof(SinglePropInput.Prop);
        sut.Inputs.Value.Const = "1";
        var action = () => pipeline.Run().WaitSeconds(1);

        //act
        await action.Should().ThrowAsync<InvalidConstantStringToTypeConversionException>();

        //assert
        sut.Completion.IsFaulted.Should().BeTrue();
    }

    public class MyClass
    {
        public string Version { get; set; } = null!;
    }
}