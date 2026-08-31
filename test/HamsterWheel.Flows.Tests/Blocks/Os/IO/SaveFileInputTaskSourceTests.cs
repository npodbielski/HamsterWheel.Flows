using FluentAssertions;
using HamsterWheel.Flows.Blocks.Os.IO;

namespace HamsterWheel.Flows.Tests.Blocks.Os.IO;

public class SaveFileInputTaskSourceTests
{
    [Fact]
    public async Task WhenAllConst_CreatesOneInstance()
    {
        //arrange
        var sut = new SaveFileInputTaskSource
        {
            Directory =
            {
                Const = "/tmp"
            },
            FileName =
            {
                Const = "test.txt"
            },
            Contents =
            {
                Const = "This is test file"
            }
        };
        //act
        var t = sut.Get().ToBlockingEnumerable().ToArray();
        //assert
        t.Should().HaveCount(1);
        t.First().Should().BeEquivalentTo(new
        {
            Directory = "/tmp",
            FileName = "test.txt",
            Contents = "This is test file",
        });
    }

    [Fact]
    public async Task WhenTwoConstAndOneList_CreatesThreeInstance()
    {
        //arrange
        var sut = new SaveFileInputTaskSource
        {
            Directory =
            {
                Const = "/tmp"
            },
            Contents =
            {
                Const = "This is test file"
            }
        };
        var list = new List<string> { "test.txt", "test1.txt", "test2.txt" };
        sut.FileName.SetSource(Task.FromResult(list));

        //act
        var t = await sut.Get().ToListAsync();

        //assert
        t.Should().HaveCount(3);
        var index = 0;
        foreach (var input in t)
        {
            input.Should().BeEquivalentTo(new
            {
                Directory = "/tmp",
                FileName = list[index++],
                Contents = "This is test file",
            });
        }
    }
    
    [Fact]
    public async Task WhenOneConstAndTwoList_CreatesTwoInstances()
    {
        //arrange
        var sut = new SaveFileInputTaskSource
        {
            Directory =
            {
                Const = "/tmp"
            }
        };
        var fileNamesList = new List<string> { "test.txt", "test1.txt", "test2.txt" };
        var contentsList = new List<string> { "This is file content", "This is the other file content" };
        sut.FileName.SetSource(Task.FromResult(fileNamesList));
        sut.Contents.SetSource(Task.FromResult(contentsList));

        //act
        var t = await sut.Get().ToListAsync();

        //assert
        t.Should().HaveCount(2);
        var index = 0;
        foreach (var input in t)
        {
            input.Should().BeEquivalentTo(new
            {
                Directory = "/tmp",
                FileName = fileNamesList[index],
                Contents = contentsList[index],
            });
            index += 1;
        }
    }
}