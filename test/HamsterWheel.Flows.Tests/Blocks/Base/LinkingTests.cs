using System.Collections.Concurrent;
using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Tests.Utils;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Tests.Blocks.Base;

public class LinkingTests
{
    [Fact]
    public async Task WhenBlocksAreLinked_ThenOutputsFromFirstAreProcessedImmediatelyBySecond()
    {
        //arrange
        var sourceBlock = new IterateBlock();
        var targetBlock = new JoinPathsBlock();
        var loggedBlockAsNumbers = new ConcurrentQueue<int>();
        var pipeline = ((IPipelineBlock[])[sourceBlock, targetBlock]).InitBlock();
        var numberOfItems = 1000;
        sourceBlock.Inputs.SetSource(DelayedIterator(numberOfItems));
        targetBlock.Inputs.First.SetSource(sourceBlock, ConverterWrapper.Converter.ConvertTo<string>);
        targetBlock.Inputs.Second.SetSource(sourceBlock, ConverterWrapper.Converter.ConvertTo<string>);
        pipeline.Logger.SetSink(m => loggedBlockAsNumbers.Enqueue(m.BlockType == nameof(JoinPathsBlock) ? 1 : 0));

        //act
        var enumerator = targetBlock.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(20);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(20);

        //assert
        var firstLogsPart = loggedBlockAsNumbers.Take(numberOfItems / 2).ToArray();
        var secondLogsPart = loggedBlockAsNumbers.Skip(numberOfItems / 2).Take(numberOfItems / 2).ToArray();
        var firstPartStd = CalculateStd(firstLogsPart);
        var secondPartStd = CalculateStd(secondLogsPart);
        //if only the first block would be operating and then the second (no parallel computation),
        // then logs converted to ints would like [0,0,0,0,0,1,1,1,1,1], so first and second half STD would be roughly equal 0
        // when they compute in parallel logs produce an array of [0,0,1,1,0,1,0,1,0,1,0,1], and both halves of it have STD
        // of approximately 0.5 
        firstPartStd.Should().BeApproximately(0.5, 0.1);
        secondPartStd.Should().BeApproximately(0.5, 0.1);
        firstPartStd.Should().BeApproximately(secondPartStd, 0.1);
        actual.Should().BeEquivalentTo(Enumerable.Range(0, numberOfItems).Select(i => $"{i}/{i}"));
    }

    [Fact]
    public async Task WhenTwoBlocksAreLinkedWithDifferentLengths_ThenOutputLengthsIsShorterInput()
    {
        //arrange
        var source1Block = new IterateBlock();
        var source2Block = new IterateBlock();
        var targetBlock = new JoinStringsBlock();
        var pipeline = ((IPipelineBlock[])[source1Block, source2Block, targetBlock]).InitBlock();
        source1Block.Inputs.Const = Enumerable.Range(0, 10);
        source2Block.Inputs.Const = Enumerable.Range(10, 40);
        targetBlock.Inputs.First.SetSource(source1Block, DefaultConverter.Instance.ConvertTo<string>);
        targetBlock.Inputs.Second.SetSource(source2Block, DefaultConverter.Instance.ConvertTo<string>);
        targetBlock.Inputs.Delimiter.Const = ":";

        //act
        var enumerator = targetBlock.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(1);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(1);

        //assert
        actual.Should()
            .BeEquivalentTo(Enumerable.Range(0, 10).Select(i => (i, i + 10)).Select(i => $"{i.i}:{i.Item2}"));
    }

    [Fact]
    public async Task WhenTwoBlocksAreLinkedToSingleInput_ThenOutputCombineBoth()
    {
        //arrange
        var source1Block = new IterateBlock();
        var source2Block = new IterateBlock();
        var targetBlock = new JoinStringsBlock();
        var pipeline = ((IPipelineBlock[])[source1Block, source2Block, targetBlock]).InitBlock();
        source1Block.Inputs.Const = Enumerable.Range(0, 5);
        source2Block.Inputs.Const = Enumerable.Range(5, 12 - 5);
        targetBlock.Inputs.First.SetSource(source1Block, DefaultConverter.Instance.ConvertTo<string>);
        targetBlock.Inputs.First.SetSource(source2Block, DefaultConverter.Instance.ConvertTo<string>);
        targetBlock.Inputs.Second.Const = ";";

        //act
        var enumerator = targetBlock.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(1);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(1);

        //assert
        actual.Should().BeEquivalentTo(Enumerable.Range(0, 12).Select(i => $"{i};"));
    }

    [Fact]
    public async Task WhenBlockHaveTowLinksAndOneIsSingle_ThenSingleLinkIsMultiplied()
    {
        //arrange
        var sourceBlock = new JoinStringsBlock();
        var source2Block = new IterateBlock();
        var targetBlock = new JoinStringsBlock { Id = "target" };
        //first source block have only single inputs and single output
        sourceBlock.Inputs.First.Const = "Hello";
        sourceBlock.Inputs.Second.Const = "World!";
        sourceBlock.Inputs.Delimiter.Const = " ";
        //second block have only multi output
        source2Block.Inputs.Const = Enumerable.Range(0, 10).Select(i => $"From me x{i}!");
        //target block
        targetBlock.Inputs.First.SetSource(sourceBlock);
        targetBlock.Inputs.Second.SetSource(source2Block, DefaultConverter.Instance.ConvertTo<string>);
        targetBlock.Inputs.Delimiter.Const = " ";
        var pipeline = ((IPipelineBlock[])[sourceBlock, source2Block, targetBlock]).InitBlock();

        //act
        var enumerator = targetBlock.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(2);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(2);

        //assert
        actual.Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(i => $"Hello World! From me x{i}!"));
    }

    private static double CalculateStd(int[] values) =>
        Math.Sqrt(values.Average(v => Math.Pow(v - values.Average(), 2)));

    private static async IAsyncEnumerable<object> DelayedIterator(int max)
    {
        foreach (var step in Enumerable.Range(0, max))
        {
            await Task.Delay(10);
            yield return (int[])[step];
        }
    }
}