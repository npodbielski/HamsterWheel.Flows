using FluentAssertions;
using HamsterWheel.Flows.Blocks.Os;

namespace HamsterWheel.Flows.Tests.Blocks.Os;

public class OsCommandInputTaskSourceTests
{
    private static string WorkingDir => AppContext.BaseDirectory;
    private static string OtherWorkingDir => Path.GetTempPath();

    [Fact]
    public async Task WhenAllConst_GetYieldsSingleInput()
    {
        //arrange
        var sut = new OsCommandInputTaskSource
        {
            Command = { Const = "echo" },
            Arguments = { Const = "hello" },
            WorkingDir = { Const = WorkingDir },
            WaitForSeconds = { Const = 5 },
            Retries = { Const = 2 }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Command.Should().Be("echo");
        result[0].Arguments.Should().Be("hello");
        result[0].WorkingDir.Should().Be(WorkingDir);
        result[0].WaitForSeconds.Should().Be(5);
        result[0].Retries.Should().Be(2);
    }

    [Fact]
    public void WhenAllConst_AllSingleIsTrue()
    {
        //arrange
        var sut = new OsCommandInputTaskSource
        {
            Command = { Const = "echo" },
            Arguments = { Const = "hello" },
            WorkingDir = { Const = WorkingDir },
            WaitForSeconds = { Const = 5 },
            Retries = { Const = 2 }
        };

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeTrue();
    }

    [Fact]
    public async Task WhenRequiredSourcesSingle_GetYieldsSingleInputWithoutOptionals()
    {
        //arrange
        var sut = new OsCommandInputTaskSource();
        sut.Command.SetSource(Task.FromResult("echo"));
        sut.WorkingDir.SetSource(Task.FromResult(WorkingDir));
        sut.WaitForSeconds.SetSource(Task.FromResult(5));

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Command.Should().Be("echo");
        result[0].WorkingDir.Should().Be(WorkingDir);
        result[0].WaitForSeconds.Should().Be(5);
        result[0].Arguments.Should().BeNull();
        result[0].Retries.Should().BeNull();
    }

    [Fact]
    public async Task WhenMultiCommandsAndWorkingDirs_GetYieldsOneInputPerCombination()
    {
        //arrange
        //the multi loop requires every source to be set (const counts as single)
        var sut = new OsCommandInputTaskSource
        {
            Arguments = { Const = "hello" },
            Retries = { Const = 1 }
        };
        sut.Command.SetSource(Commands());
        sut.WorkingDir.SetSource(WorkingDirs());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Command.Should().Be("echo-a");
        result[0].WorkingDir.Should().Be(WorkingDir);
        result[1].Command.Should().Be("echo-b");
        result[1].WorkingDir.Should().Be(OtherWorkingDir);
        result.Should().AllSatisfy(i =>
        {
            i.WaitForSeconds.Should().Be(30);
            i.Arguments.Should().Be("hello");
            i.Retries.Should().Be(1);
        });
    }

    [Fact]
    public async Task WhenMultiArguments_GetYieldsArgumentsPerCombination()
    {
        //arrange
        var sut = new OsCommandInputTaskSource
        {
            Retries = { Const = 1 }
        };
        sut.Command.SetSource(Commands());
        sut.WorkingDir.SetSource(WorkingDirs());
        sut.Arguments.SetSource(Arguments());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Arguments.Should().Be("arg-1");
        result[1].Arguments.Should().Be("arg-2");
        result.Should().AllSatisfy(i => i.WaitForSeconds.Should().Be(30));
    }

    [Fact]
    public async Task WhenMultiAllSettableSources_GetYieldsCombinationsUntilShortestIsExhausted()
    {
        //arrange
        //WaitForSeconds keeps its default const value: SetSource(IAsyncEnumerable) does not clear the const flag
        var sut = new OsCommandInputTaskSource();
        sut.Command.SetSource(Commands());
        sut.WorkingDir.SetSource(WorkingDirs());
        sut.Arguments.SetSource(Arguments());
        sut.Retries.SetSource(Retries());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Command.Should().Be("echo-a");
        result[0].WorkingDir.Should().Be(WorkingDir);
        result[0].WaitForSeconds.Should().Be(30);
        result[0].Arguments.Should().Be("arg-1");
        result[0].Retries.Should().Be(1);
        result[1].Command.Should().Be("echo-b");
        result[1].WorkingDir.Should().Be(OtherWorkingDir);
        result[1].WaitForSeconds.Should().Be(30);
        result[1].Arguments.Should().Be("arg-2");
        result[1].Retries.Should().Be(2);
    }

    private static async IAsyncEnumerable<string> Commands()
    {
        yield return "echo-a";
        await Task.Yield();
        yield return "echo-b";
    }

    private static async IAsyncEnumerable<string> WorkingDirs()
    {
        yield return WorkingDir;
        await Task.Yield();
        yield return OtherWorkingDir;
    }

    private static async IAsyncEnumerable<string> Arguments()
    {
        yield return "arg-1";
        await Task.Yield();
        yield return "arg-2";
        yield return "arg-3";
    }

    private static async IAsyncEnumerable<int?> Retries()
    {
        yield return 1;
        await Task.Yield();
        yield return 2;
        yield return 3;
    }
}
