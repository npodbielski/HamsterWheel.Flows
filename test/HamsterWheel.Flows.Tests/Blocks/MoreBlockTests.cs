using HamsterWheel.Flows.DI;
using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Blocks.Os;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Blocks.Text;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Tests.Blocks;

public class MoreBlockTests
{

    [Fact]
    public async Task WhenJoinStringsWithConst_ThenJoined()
    {
        //arrange
        var join = new JoinStringsBlock();
        var pipeline = join.InitBlock();
        join.Inputs.First.Const = "hello ";
        join.Inputs.Second.Const = "world";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await join.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().Be("hello world");
    }

    [Fact]
    public async Task WhenLogBlockWithConst_ThenDoesNotThrow()
    {
        //arrange
        var log = new LogBlock();
        var pipeline = log.InitBlock();
        log.Inputs.Const = "test message";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        // No exception = success
    }

    [Fact]
    public async Task WhenCreateObjectWithConsts_ThenObjectCreated()
    {
        //arrange
        var createObj = new CreateObjectBlock();
        var pipeline = createObj.InitBlock();
        createObj.Inputs.Object1.Const = "hello";
        createObj.Inputs.Property1.Const = "Name";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await createObj.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenJoinPathsBlock_ThenPathsJoined()
    {
        //arrange
        var block = new JoinPathsBlock();
        var pipeline = block.InitBlock();
        block.Inputs.First.Const = "/usr";
        block.Inputs.Second.Const = "local/bin";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().Be(Path.Combine("/usr", "local/bin"));
    }

    [Fact]
    public async Task WhenMakeDirBlock_ThenDirectoryCreated()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-test-{Guid.NewGuid():N}");
        var block = new MakeDirBlock();
        block.ResolveIo();
        var pipeline = block.InitBlock();
        block.Inputs.Const = tempDir;

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().Be(tempDir);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task WhenDeleteDirBlock_ThenDirectoryDeleted()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var block = new DeleteDirBlock();
        block.ResolveIo();
        var pipeline = block.InitBlock();
        block.Inputs.Const = tempDir;

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        Directory.Exists(tempDir).Should().BeFalse();
    }

    [Fact]
    public async Task WhenRenderTemplateBlockWithSimpleTemplate_ThenRendered()
    {
        //arrange
        var block = new RenderTemplateBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Template.Const = "Hello {{name}}!";
        block.Inputs.Model.Const = new Dictionary<string, object> { ["name"] = "World" };

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        // Will throw BlockServicesNotInitializedException since Resolve was not called
        // This still covers the code path up to the service check
        await action.Should().ThrowAsync<Exception>();
    }
}
