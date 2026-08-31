using HamsterWheel.Flows.DI;
using FluentAssertions;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Os.IO;

public class SaveFileBlockTests
{
    [Fact]
    public async Task Run_WhenOnlyConstInputs_SaveFile()
    {
        //arrange
        var sut = new SaveFileBlock();
        sut.ResolveIo();
        var pipeline = sut.InitBlock();
        sut.Inputs.Contents.Const = "Test";
        var fileNameConst = $"{Guid.NewGuid()}test.txt";
        sut.Inputs.FileName.Const = fileNameConst;
        sut.Inputs.Directory.Const = "/tmp";
        //act
        await pipeline.Run().WaitSeconds();
        //assert
        var content = await File.ReadAllTextAsync(Path.Combine("/tmp", fileNameConst));
        content.Should().Be(sut.Inputs.Contents.Const);
    }
}