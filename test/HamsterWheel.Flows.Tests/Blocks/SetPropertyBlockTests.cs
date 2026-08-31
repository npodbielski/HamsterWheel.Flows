using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class SetPropertyBlockTests
{
    [Fact]
    public async Task WhenPropertyPathAndValue_ThenPropertyIsSetOnObject()
    {
        //arrange
        var target = new MutableTarget { Name = "old" };
        var block = new SetPropertyBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Object.Const = target;
        block.Inputs.PropertyPath.Const = "Name";
        block.Inputs.Value.Const = "new";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        target.Name.Should().Be("new");
    }

    [Fact]
    public async Task WhenNoPropertyPathAndNoMap_ThenEntireObjectIsMapped()
    {
        //arrange
        var target = new MutableTarget { Name = "old" };
        var block = new SetPropertyBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Object.Const = target;
        block.Inputs.PropertyPath.Const = "";
        block.Inputs.Value.Const = null;

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().NotThrowAsync();
    }

    private class MutableTarget
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
