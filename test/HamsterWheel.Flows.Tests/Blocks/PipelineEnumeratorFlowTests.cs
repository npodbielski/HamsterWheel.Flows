using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Tests.Utils;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Tests.Blocks;

public class PipelineEnumeratorFlowTests
{
    [Fact]
    public async Task WhenIterateManyItemsToJoinStrings_ThenAllResultsDelivered()
    {
        //arrange
        var items = new[] { "one", "two", "three", "four", "five", "six", "seven", "eight" };
        var iterate = new IterateBlock();
        var join = new JoinStringsBlock();
        var pipeline = ((IPipelineBlock[])[iterate, join]).InitBlock();
        iterate.Inputs.Const = items;
        join.Inputs.First.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        join.Inputs.Second.Const = "!";
        var enumerator = join.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().HaveCount(8);
        actual[0].Should().Be("one!");
        actual[7].Should().Be("eight!");
    }

    [Fact]
    public async Task WhenThreeBlockChain_ThenAllOutputsFlowThrough()
    {
        //arrange
        var iterate = new IterateBlock();
        var join = new JoinStringsBlock();
        var join2 = new JoinStringsBlock();
        var pipeline = ((IPipelineBlock[])[iterate, join, join2]).InitBlock();
        iterate.Inputs.Const = new[] { "a", "b", "c" };
        join.Inputs.First.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        join.Inputs.Second.Const = "-x";
        join2.Inputs.First.SetSource(join);
        join2.Inputs.Second.Const = "-y";
        var enumerator = join2.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().BeEquivalentTo(["a-x-y", "b-x-y", "c-x-y"]);
    }

    [Fact]
    public async Task WhenIterateToCreateObject_ThenAllObjectsDelivered()
    {
        //arrange
        var iterate = new IterateBlock();
        var createObj = new CreateObjectBlock();
        var pipeline = ((IPipelineBlock[])[iterate, createObj]).InitBlock();
        iterate.Inputs.Const = new object[] { "val1", "val2", "val3" };
        createObj.Inputs.Object1.SetSource(iterate);
        createObj.Inputs.Property1.Const = "Name";
        var enumerator = createObj.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().HaveCount(3);
    }

    [Fact]
    public async Task WhenIterateToLogBlock_ThenAllItemsProcessed()
    {
        //arrange
        var iterate = new IterateBlock();
        var join = new JoinStringsBlock();
        var log = new LogBlock();
        var pipeline = ((IPipelineBlock[])[iterate, join, log]).InitBlock();
        iterate.Inputs.Const = new[] { "log1", "log2", "log3", "log4" };
        join.Inputs.First.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        join.Inputs.Second.Const = ">>";
        log.Inputs.SetSource(join);

        //act
        await pipeline.Run().WaitSeconds(10);

        //assert
        // No exception = all items flowed through the chain to the log block
    }

    [Fact]
    public async Task WhenIterateToMakeDir_ThenAllDirsCreated()
    {
        //arrange
        var tempBase = Path.Combine(Path.GetTempPath(), $"flows-enum-{Guid.NewGuid():N}");
        var dirs = new[] { "d1", "d2", "d3" };
        var iterate = new IterateBlock();
        var makeDir = new MakeDirBlock();
        makeDir.ResolveIo();
        var pipeline = ((IPipelineBlock[])[iterate, makeDir]).InitBlock();
        iterate.Inputs.Const = dirs.Select(d => Path.Combine(tempBase, d)).ToArray();
        makeDir.Inputs.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        var enumerator = makeDir.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(10);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().HaveCount(3);
        Directory.Exists(Path.Combine(tempBase, "d1")).Should().BeTrue();
        Directory.Exists(Path.Combine(tempBase, "d2")).Should().BeTrue();
        Directory.Exists(Path.Combine(tempBase, "d3")).Should().BeTrue();
        Directory.Delete(tempBase, true);
    }

    [Fact]
    public async Task WhenSingleItemThroughChain_ThenSingleResult()
    {
        //arrange
        var iterate = new IterateBlock();
        var join = new JoinStringsBlock();
        var pipeline = ((IPipelineBlock[])[iterate, join]).InitBlock();
        iterate.Inputs.Const = new[] { "only" };
        join.Inputs.First.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);
        join.Inputs.Second.Const = "!";
        var enumerator = join.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().ContainSingle().Which.Should().Be("only!");
    }

    [Fact]
    public async Task WhenIterateToSetProperty_ThenAllObjectsModified()
    {
        //arrange
        var iterate = new IterateBlock();
        var createObj = new CreateObjectBlock();
        var setProp = new SetPropertyBlock();
        var pipeline = ((IPipelineBlock[])[iterate, createObj, setProp]).InitBlock();
        iterate.Inputs.Const = new object[] { "obj1", "obj2" };
        createObj.Inputs.Object1.SetSource(iterate);
        createObj.Inputs.Property1.Const = "Value";
        setProp.Inputs.Object.SetSource(createObj);
        setProp.Inputs.PropertyPath.Const = "Value";
        setProp.Inputs.Value.Const = "modified";
        var enumerator = setProp.Result.AsEnumerable();

        //act
        await pipeline.Run().WaitSeconds(10);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().HaveCount(2);
    }
}
