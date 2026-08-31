using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class CreateObjectBlockTests
{
    [Fact]
    public async Task WhenSingleProperty_ThenCreatesObjectWithThatProperty()
    {
        //arrange
        var block = new CreateObjectBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Property1.Const = "Name";
        block.Inputs.Object1.Const = "John";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().NotBeNull();
        var prop = result!.GetType().GetProperty("Name");
        prop.Should().NotBeNull();
        prop!.GetValue(result).Should().Be("John");
    }

    [Fact]
    public async Task WhenTwoProperties_ThenCreatesObjectWithBoth()
    {
        //arrange
        var block = new CreateObjectBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Property1.Const = "First";
        block.Inputs.Object1.Const = "John";
        block.Inputs.Property2.Const = "Last";
        block.Inputs.Object2.Const = "Doe";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().NotBeNull();
        var first = result!.GetType().GetProperty("First");
        var last = result!.GetType().GetProperty("Last");
        first!.GetValue(result).Should().Be("John");
        last!.GetValue(result).Should().Be("Doe");
    }

    [Fact]
    public async Task WhenThreeProperties_ThenCreatesObjectWithAll()
    {
        //arrange
        var block = new CreateObjectBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Property1.Const = "A";
        block.Inputs.Object1.Const = 1;
        block.Inputs.Property2.Const = "B";
        block.Inputs.Object2.Const = "two";
        block.Inputs.Property3.Const = "C";
        block.Inputs.Object3.Const = 3.0;

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result!.GetType().GetProperty("A")!.GetValue(result).Should().Be(1);
        result!.GetType().GetProperty("B")!.GetValue(result).Should().Be("two");
        result!.GetType().GetProperty("C")!.GetValue(result).Should().Be(3.0);
    }
}
