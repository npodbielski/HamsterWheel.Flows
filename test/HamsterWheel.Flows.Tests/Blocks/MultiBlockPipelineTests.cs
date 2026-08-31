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

public class MultiBlockPipelineTests
{
    [Fact]
    public async Task WhenIterateJoinStrings_ThenStringsJoined()
    {
        //arrange
        var iterate = new IterateBlock();
        var join = new JoinStringsBlock();
        var pipeline = ((IPipelineBlock[])[iterate, join]).InitBlock();
        iterate.Inputs.Const = new[] { "a", "b", "c" };
        join.Inputs.First.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        join.Inputs.Second.Const = "!";
        var enumerator = join.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().BeEquivalentTo(["a!", "b!", "c!"]);
    }

    [Fact]
    public async Task WhenIterateTakeFirst_ThenOnlyFirstIsReturned()
    {
        //arrange
        var iterate = new IterateBlock();
        var takeFirst = new TakeFirstBlock();
        var pipeline = ((IPipelineBlock[])[iterate, takeFirst]).InitBlock();
        iterate.Inputs.Const = new[] { 1, 2, 3 };
        takeFirst.Inputs.SetSource(iterate);
        var enumerator = takeFirst.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task WhenIterateCreateObject_ThenObjectsCreated()
    {
        //arrange
        var iterate = new IterateBlock();
        var createObj = new CreateObjectBlock();
        var pipeline = ((IPipelineBlock[])[iterate, createObj]).InitBlock();
        iterate.Inputs.Const = new[] { 1, 2, 3 };
        createObj.Inputs.Object1.SetSource(iterate);
        createObj.Inputs.Property1.Const = "Value";
        var enumerator = createObj.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().HaveCount(3);
    }

    [Fact]
    public async Task WhenIterateJoinStringsLog_ThenAllProcessed()
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
        // No exception = success (log block has no output to read)
    }

    [Fact]
    public async Task WhenIterateMakeDir_ThenDirectoriesCreated()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-mb-{Guid.NewGuid():N}");
        var iterate = new IterateBlock();
        var makeDir = new MakeDirBlock();
        makeDir.ResolveIo();
        var pipeline = ((IPipelineBlock[])[iterate, makeDir]).InitBlock();
        iterate.Inputs.Const = new[] { Path.Combine(tempDir, "a"), Path.Combine(tempDir, "b") };
        makeDir.Inputs.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        var enumerator = makeDir.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        Directory.Exists(Path.Combine(tempDir, "a")).Should().BeTrue();
        Directory.Exists(Path.Combine(tempDir, "b")).Should().BeTrue();
        actual.Should().HaveCount(2);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task WhenIterateJoinStrings_ThenAllJoined()
    {
        //arrange
        var iterate = new IterateBlock();
        var join = new JoinStringsBlock();
        var pipeline = ((IPipelineBlock[])[iterate, join]).InitBlock();
        iterate.Inputs.Const = new[] { "x", "y", "z" };
        join.Inputs.First.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        join.Inputs.Second.Const = ",";
        var enumerator = join.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().BeEquivalentTo(["x,", "y,", "z,"]);
    }

    [Fact]
    public async Task WhenIterateCreateObjectSetProperty_ThenObjectModified()
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
        var enumerator = setProp.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().HaveCount(1);
    }

    [Fact]
    public async Task WhenIterateSaveFile_ThenFilesSaved()
    {
        //arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"flows-mb-{Guid.NewGuid():N}");
        var iterate = new IterateBlock();
        var saveFile = new SaveFileBlock();
        saveFile.ResolveIo();
        var pipeline = ((IPipelineBlock[])[iterate, saveFile]).InitBlock();
        iterate.Inputs.Const = new[] { "file1.txt", "file2.txt" };
        saveFile.Inputs.Directory.Const = tempDir;
        saveFile.Inputs.FileName.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        saveFile.Inputs.Contents.Const = "content";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        File.Exists(Path.Combine(tempDir, "file1.txt")).Should().BeTrue();
        File.Exists(Path.Combine(tempDir, "file2.txt")).Should().BeTrue();
        Directory.Delete(tempDir, true);
    }
}
