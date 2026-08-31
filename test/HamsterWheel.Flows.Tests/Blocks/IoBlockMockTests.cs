using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Blocks;

public class IoBlockMockTests
{
    [Fact]
    public async Task WhenCopyFilesBlockWithMockedFS_ThenFilesAreCopied()
    {
        //arrange
        var fs = Substitute.For<IFileSystem>();
        fs.GetFiles("/src", "*").Returns(["/src/a.txt", "/src/b.txt"]);
        var block = new CopyFilesBlock();
        ResolveWith(block, fs);
        var pipeline = block.InitBlock();
        block.Inputs.SourcePath.Const = "/src";
        block.Inputs.TargetPath.Const = "/dst";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        fs.Received(1).CreateDirectory("/dst");
        fs.Received(1).CopyFile("/src/a.txt", Path.Combine("/dst", "a.txt"), true);
        fs.Received(1).CopyFile("/src/b.txt", Path.Combine("/dst", "b.txt"), true);
    }

    [Fact]
    public async Task WhenCopyFilesBlockRecursive_ThenSubdirectoriesProcessed()
    {
        //arrange
        var fs = Substitute.For<IFileSystem>();
        fs.GetFiles("/src", "*").Returns(["/src/a.txt"]);
        fs.GetFiles("/src/sub", "*").Returns(["/src/sub/c.txt"]);
        fs.EnumerateDirectories("/src").Returns(["/src/sub"]);
        fs.EnumerateDirectories("/src/sub").Returns([]);
        var block = new CopyFilesBlock();
        ResolveWith(block, fs);
        var pipeline = block.InitBlock();
        block.Inputs.SourcePath.Const = "/src";
        block.Inputs.TargetPath.Const = "/dst";
        block.Inputs.Recursive.Const = true;

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        fs.Received(1).CopyFile("/src/a.txt", Path.Combine("/dst", "a.txt"), true);
        fs.Received(1).CopyFile("/src/sub/c.txt", Path.Combine("/dst", "sub", "c.txt"), true);
    }

    [Fact]
    public async Task WhenSaveFileBlockWithMockedFS_ThenFileIsWritten()
    {
        //arrange
        var fs = Substitute.For<IFileSystem>();
        fs.DirectoryExists("/target").Returns(true);
        var stream = new MemoryStream();
        fs.OpenFile(Path.Combine("/target", "test.txt"), FileMode.Create).Returns(stream);
        var block = new SaveFileBlock();
        ResolveWith(block, fs);
        var pipeline = block.InitBlock();
        block.Inputs.Directory.Const = "/target";
        block.Inputs.FileName.Const = "test.txt";
        block.Inputs.Contents.Const = "hello world";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        fs.Received(1).OpenFile(Path.Combine("/target", "test.txt"), FileMode.Create);
    }

    [Fact]
    public async Task WhenSaveFileBlockDirDoesNotExist_ThenDirIsCreated()
    {
        //arrange
        var fs = Substitute.For<IFileSystem>();
        fs.DirectoryExists("/newdir").Returns(false);
        var stream = new MemoryStream();
        fs.OpenFile(Path.Combine("/newdir", "f.txt"), FileMode.Create).Returns(stream);
        var block = new SaveFileBlock();
        ResolveWith(block, fs);
        var pipeline = block.InitBlock();
        block.Inputs.Directory.Const = "/newdir";
        block.Inputs.FileName.Const = "f.txt";
        block.Inputs.Contents.Const = "data";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        fs.Received(1).CreateDirectory("/newdir");
    }

    [Fact]
    public async Task WhenMakeDirBlockWithMockedFS_ThenDirIsCreated()
    {
        //arrange
        var fs = Substitute.For<IFileSystem>();
        var block = new MakeDirBlock();
        ResolveWith(block, fs);
        var pipeline = block.InitBlock();
        block.Inputs.Const = "/some/path";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        fs.Received(1).CreateDirectory("/some/path");
        result.Should().Be("/some/path");
    }

    [Fact]
    public async Task WhenDeleteDirBlockExists_ThenDeleted()
    {
        //arrange
        var fs = Substitute.For<IFileSystem>();
        fs.PathExists("/to/delete").Returns(true);
        var block = new DeleteDirBlock();
        ResolveWith(block, fs);
        var pipeline = block.InitBlock();
        block.Inputs.Const = "/to/delete";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        fs.Received(1).DeleteDirectory("/to/delete", true);
    }

    [Fact]
    public async Task WhenDeleteDirBlockDoesNotExist_ThenNothingHappens()
    {
        //arrange
        var fs = Substitute.For<IFileSystem>();
        fs.PathExists("/missing").Returns(false);
        var block = new DeleteDirBlock();
        ResolveWith(block, fs);
        var pipeline = block.InitBlock();
        block.Inputs.Const = "/missing";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        fs.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
    }

    private static void ResolveWith(INeedServices block, IFileSystem fs)
    {
        var services = new ServiceCollection()
            .AddSingleton<IFileSystem>(fs)
            .BuildServiceProvider();
        block.Resolve(services);
    }
}
