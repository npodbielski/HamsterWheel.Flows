using FluentAssertions;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class CreateObjectBlockTests
{
    [Fact]
    public async Task WhenSingleProperty_ThenCreatesNewTypeWithOneProperty()
    {
        //arrange
        var expected = "5.0";
        var sut = new CreateObjectBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Property1.Const = "version";
        sut.Inputs.Object1.Const = expected;

        //act
        await pipeline.Run().WaitSeconds(1);
        dynamic actual = await sut.Result.SingleValue;

        //assert
        ((object)actual).Should().NotBeNull();
        ((string)actual.Version).Should().Be(expected);
    }

    [Fact]
    public async Task WhenTwoProperties_ThenCreatesNewTypeWithTwoProperties()
    {
        //arrange
        var expectedVersion = "5.0";
        var expectedName = "testExtension";
        var sut = new CreateObjectBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Property1.Const = "version";
        sut.Inputs.Object1.Const = expectedVersion;
        sut.Inputs.Property2.Const = "name";
        sut.Inputs.Object2.Const = expectedName;

        //act
        await pipeline.Run().WaitSeconds(1);
        dynamic actual = await sut.Result.SingleValue;

        //assert
        ((object)actual).Should().NotBeNull();
        ((string)actual.Version).Should().Be(expectedVersion);
        ((string)actual.Name).Should().Be(expectedName);
    }

    [Fact]
    public async Task WhenTwoPropertiesOneLinked_ThenCreatesNewTypeWithTwoProperties()
    {
        //arrange
        var expectedVersion = "5.0";
        var expectedName = "testExtension";
        var sut = new CreateObjectBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Property1.Const = "version";
        sut.Inputs.Object1.SetSource(Task.FromResult(new object[] { expectedVersion }));
        sut.Inputs.Property2.Const = "name";
        sut.Inputs.Object2.Const = expectedName;

        //act
        var enumerator = sut.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(1);
        dynamic? actual = await enumerator.FirstOrDefaultAsync().AsTask().WaitSeconds(1);

        //assert
        ((object?)actual).Should().NotBeNull();
        ((string)actual.Version).Should().Be(expectedVersion);
        ((string)actual.Name).Should().Be(expectedName);
    }

    [Fact]
    public async Task WhenThreeProperties_ThenCreatesNewTypeWithThreeProperties()
    {
        //arrange
        var expectedVersion = "5.0";
        var expectedPrevVersion = "3.0";
        var expectedName = "testExtension";
        var sut = new CreateObjectBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Property1.Const = "version";
        sut.Inputs.Object1.Const = expectedVersion;
        sut.Inputs.Property2.Const = "prevVersion";
        sut.Inputs.Object2.Const = expectedPrevVersion;
        sut.Inputs.Property3.Const = "name";
        sut.Inputs.Object3.Const = expectedName;

        //act
        await pipeline.Run().WaitSeconds(1);
        dynamic actual = await sut.Result.SingleValue;

        //assert
        ((object)actual).Should().NotBeNull();
        ((string)actual.Version).Should().Be(expectedVersion);
        ((string)actual.Name).Should().Be(expectedName);
        ((string)actual.PrevVersion).Should().Be(expectedPrevVersion);
    }
}
