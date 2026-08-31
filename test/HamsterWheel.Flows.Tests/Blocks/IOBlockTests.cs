using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class JoinPathsBlockTests
{
    [Fact]
    public async Task WhenTwoPathsJoined_ThenReturnsCombinedPath()
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
    public async Task WhenSingleSegmentPaths_ThenReturnsCombined()
    {
        //arrange
        var block = new JoinPathsBlock();
        var pipeline = block.InitBlock();
        block.Inputs.First.Const = "src";
        block.Inputs.Second.Const = "main.cs";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().Be(Path.Combine("src", "main.cs"));
    }
}

public class SaveFileBlockTests
{
    [Fact]
    public async Task WhenSaveFile_ThenFileIsCreatedWithContent()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-save-{Guid.NewGuid():N}");
        var block = new SaveFileBlock();
        block.ResolveIo();
        var pipeline = block.InitBlock();
        block.Inputs.Directory.Const = tempDir;
        block.Inputs.FileName.Const = "test.txt";
        block.Inputs.Contents.Const = "Hello File";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        var filePath = Path.Combine(tempDir, "test.txt");
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be("Hello File");
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task WhenSaveFileWithAbsolutePath_ThenLeadingSlashIsStripped()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-save-{Guid.NewGuid():N}");
        var block = new SaveFileBlock();
        block.ResolveIo();
        var pipeline = block.InitBlock();
        block.Inputs.Directory.Const = tempDir;
        block.Inputs.FileName.Const = "/absolute.txt";
        block.Inputs.Contents.Const = "content";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        var filePath = Path.Combine(tempDir, "absolute.txt");
        File.Exists(filePath).Should().BeTrue();
        Directory.Delete(tempDir, true);
    }
}

public class CopyFilesBlockTests
{
    [Fact]
    public async Task WhenCopyFiles_ThenFilesAreCopiedToTarget()
    {
        //arrange
        var sourceDir = Path.Combine(Path.GetTempPath(), $"flows-copy-src-{Guid.NewGuid():N}");
        var targetDir = Path.Combine(Path.GetTempPath(), $"flows-copy-dst-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "a.txt"), "aaa");
        File.WriteAllText(Path.Combine(sourceDir, "b.txt"), "bbb");

        var block = new CopyFilesBlock();
        block.ResolveIo();
        var pipeline = block.InitBlock();
        block.Inputs.SourcePath.Const = sourceDir;
        block.Inputs.TargetPath.Const = targetDir;

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        File.Exists(Path.Combine(targetDir, "a.txt")).Should().BeTrue();
        File.Exists(Path.Combine(targetDir, "b.txt")).Should().BeTrue();
        File.ReadAllText(Path.Combine(targetDir, "a.txt")).Should().Be("aaa");
        Directory.Delete(sourceDir, true);
        Directory.Delete(targetDir, true);
    }

    [Fact]
    public async Task WhenCopyFilesWithFilter_ThenOnlyMatchingFilesCopied()
    {
        //arrange
        var sourceDir = Path.Combine(Path.GetTempPath(), $"flows-copy-src-{Guid.NewGuid():N}");
        var targetDir = Path.Combine(Path.GetTempPath(), $"flows-copy-dst-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "a.txt"), "aaa");
        File.WriteAllText(Path.Combine(sourceDir, "b.csv"), "bbb");

        var block = new CopyFilesBlock();
        block.ResolveIo();
        var pipeline = block.InitBlock();
        block.Inputs.SourcePath.Const = sourceDir;
        block.Inputs.TargetPath.Const = targetDir;
        block.Inputs.Filter.Const = "*.txt";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        File.Exists(Path.Combine(targetDir, "a.txt")).Should().BeTrue();
        File.Exists(Path.Combine(targetDir, "b.csv")).Should().BeFalse();
        Directory.Delete(sourceDir, true);
        Directory.Delete(targetDir, true);
    }
}
