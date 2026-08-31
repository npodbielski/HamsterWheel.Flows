using HamsterWheel.Flows.DI;
using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class MakeDirBlockTests
{
    [Fact]
    public async Task WhenDirectoryIsCreated_ThenReturnsPath()
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
        Directory.Exists(tempDir).Should().BeTrue();
        Directory.Delete(tempDir, true);
    }
}

public class DeleteDirBlockTests
{
    [Fact]
    public async Task WhenDirectoryExists_ThenItIsDeleted()
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
    public async Task WhenDirectoryDoesNotExist_ThenNoException()
    {
        //arrange
        var nonExistent = Path.Combine(Path.GetTempPath(), $"flows-noexist-{Guid.NewGuid():N}");
        var block = new DeleteDirBlock();
        block.ResolveIo();
        var pipeline = block.InitBlock();
        block.Inputs.Const = nonExistent;

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().NotThrowAsync();
    }
}
