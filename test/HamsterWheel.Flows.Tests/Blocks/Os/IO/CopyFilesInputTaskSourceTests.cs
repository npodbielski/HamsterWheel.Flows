using FluentAssertions;
using HamsterWheel.Flows.Blocks.Os.IO;

namespace HamsterWheel.Flows.Tests.Blocks.Os.IO;

public class CopyFilesInputTaskSourceTests
{
    [Fact]
    public async Task WhenAllConst_GetYieldsSingleInput()
    {
        //arrange
        var sut = new CopyFilesInputTaskSource
        {
            SourcePath = { Const = "/source" },
            TargetPath = { Const = "/target" },
            Recursive = { Const = false }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].SourcePath.Should().Be("/source");
        result[0].TargetPath.Should().Be("/target");
        result[0].Recursive.Should().BeFalse();
        result[0].Filter.Should().BeNull();
    }

    [Fact]
    public void WhenAllConst_AllSingleIsTrue()
    {
        //arrange
        var sut = new CopyFilesInputTaskSource
        {
            SourcePath = { Const = "/source" },
            TargetPath = { Const = "/target" }
        };

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeTrue();
    }

    [Fact]
    public async Task WhenSingleTaskSources_GetYieldsSingleInput()
    {
        //arrange
        var sut = new CopyFilesInputTaskSource();
        sut.SourcePath.SetSource(Task.FromResult("/source"));
        sut.TargetPath.SetSource(Task.FromResult("/target"));
        sut.Recursive.SetSource(Task.FromResult(true));
        sut.Filter.SetSource(Task.FromResult<string?>("*.txt"));

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].SourcePath.Should().Be("/source");
        result[0].TargetPath.Should().Be("/target");
        result[0].Recursive.Should().BeTrue();
        result[0].Filter.Should().Be("*.txt");
    }

    [Fact]
    public async Task WhenMultiSourceAndTargetPaths_GetYieldsCombinationsUntilShortestIsExhausted()
    {
        //arrange
        var sut = new CopyFilesInputTaskSource();
        sut.SourcePath.SetSource(SourcePaths());
        sut.TargetPath.SetSource(TargetPaths());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].SourcePath.Should().Be("/source-a");
        result[0].TargetPath.Should().Be("/target-a");
        result[1].SourcePath.Should().Be("/source-b");
        result[1].TargetPath.Should().Be("/target-b");
        result.Should().AllSatisfy(i =>
        {
            i.Recursive.Should().BeTrue();
            i.Filter.Should().BeNull();
        });
    }

    [Fact]
    public async Task WhenSourcePathConstAndTargetPathMulti_GetYieldsOneInputPerTarget()
    {
        //arrange
        var sut = new CopyFilesInputTaskSource
        {
            SourcePath = { Const = "/source" }
        };
        sut.TargetPath.SetSource(TargetPaths());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(3);
        result[0].TargetPath.Should().Be("/target-a");
        result[1].TargetPath.Should().Be("/target-b");
        result[2].TargetPath.Should().Be("/target-c");
        result.Should().AllSatisfy(i =>
        {
            i.SourcePath.Should().Be("/source");
            i.Recursive.Should().BeTrue();
            i.Filter.Should().BeNull();
        });
    }

    [Fact]
    public async Task WhenTargetPathConstAndSourcePathMulti_GetYieldsOneInputPerSource()
    {
        //arrange
        var sut = new CopyFilesInputTaskSource
        {
            TargetPath = { Const = "/target" }
        };
        sut.SourcePath.SetSource(SourcePaths());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].SourcePath.Should().Be("/source-a");
        result[1].SourcePath.Should().Be("/source-b");
        result.Should().AllSatisfy(i =>
        {
            i.TargetPath.Should().Be("/target");
            i.Recursive.Should().BeTrue();
            i.Filter.Should().BeNull();
        });
    }

    private static async IAsyncEnumerable<string> SourcePaths()
    {
        yield return "/source-a";
        await Task.Yield();
        yield return "/source-b";
    }

    private static async IAsyncEnumerable<string> TargetPaths()
    {
        yield return "/target-a";
        await Task.Yield();
        yield return "/target-b";
        yield return "/target-c";
    }

}
