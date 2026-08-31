using HamsterWheel.Flows.DI;
using System.Collections.Concurrent;
using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Tests.Utils;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Tests.Blocks;

public class IntegrationPipelineTests
{
    [Fact]
    public async Task WhenIterateCreateObjectAndSetProperty_ThenObjectIsModified()
    {
        //arrange
        var iterate = new IterateBlock();
        var createObj = new CreateObjectBlock();
        var setProp = new SetPropertyBlock();
        var pipeline = ((IPipelineBlock[])[iterate, createObj, setProp]).InitBlock();
        iterate.Inputs.Const = new[] { 1 };
        createObj.Inputs.Object1.SetSource(iterate);
        createObj.Inputs.Property1.Const = "Value";
        setProp.Inputs.Object.SetSource(createObj);
        setProp.Inputs.PropertyPath.Const = "Value";
        setProp.Inputs.Value.Const = 42;

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        // No exception = success
    }

    [Fact]
    public async Task WhenIterateJoinStringsLog_ThenLogsAreWritten()
    {
        //arrange
        var iterate = new IterateBlock();
        var join = new JoinStringsBlock();
        var log = new LogBlock();
        var pipeline = ((IPipelineBlock[])[iterate, join, log]).InitBlock();
        iterate.Inputs.Const = new[] { "hello", "world" };
        join.Inputs.First.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        join.Inputs.Second.Const = "!";
        log.Inputs.SetSource(join);

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        // No exception = success
    }

    [Fact]
    public async Task WhenIterateMakeDir_ThenDirectoriesCreated()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-int-{Guid.NewGuid():N}");
        var iterate = new IterateBlock();
        var makeDir = new MakeDirBlock();
        makeDir.ResolveIo();
        var pipeline = ((IPipelineBlock[])[iterate, makeDir]).InitBlock();
        iterate.Inputs.Const = new[] { Path.Combine(tempDir, "a"), Path.Combine(tempDir, "b") };
        makeDir.Inputs.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        Directory.Exists(Path.Combine(tempDir, "a")).Should().BeTrue();
        Directory.Exists(Path.Combine(tempDir, "b")).Should().BeTrue();
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task WhenIterateDeleteDir_ThenDirectoriesDeleted()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-int-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "to-delete"));
        var iterate = new IterateBlock();
        var deleteDir = new DeleteDirBlock();
        deleteDir.ResolveIo();
        var pipeline = ((IPipelineBlock[])[iterate, deleteDir]).InitBlock();
        iterate.Inputs.Const = new[] { Path.Combine(tempDir, "to-delete") };
        deleteDir.Inputs.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        Directory.Exists(Path.Combine(tempDir, "to-delete")).Should().BeFalse();
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task WhenIterateSaveFile_ThenFilesSaved()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-int-{Guid.NewGuid():N}");
        var iterate = new IterateBlock();
        var saveFile = new SaveFileBlock();
        saveFile.ResolveIo();
        var pipeline = ((IPipelineBlock[])[iterate, saveFile]).InitBlock();
        iterate.Inputs.Const = new[] { "file1.txt" };
        saveFile.Inputs.Directory.Const = tempDir;
        saveFile.Inputs.FileName.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        saveFile.Inputs.Contents.Const = "content";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        File.Exists(Path.Combine(tempDir, "file1.txt")).Should().BeTrue();
        Directory.Delete(tempDir, true);
    }
}
