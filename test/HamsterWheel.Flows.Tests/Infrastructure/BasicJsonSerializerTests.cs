using FluentAssertions;
using HamsterWheel.Flows.Data.Serialization;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class BasicJsonSerializerTests
{
    [Fact]
    public void WhenSerializingObject_ThenReturnsJson()
    {
        //arrange
        var serializer = new BasicJsonSerializer();
        var obj = new TestDto("test", 42);

        //act
        var result = serializer.Serialize(obj);

        //assert
        result.Should().Contain("\"Name\":\"test\"");
        result.Should().Contain("\"Value\":42");
    }

    [Fact]
    public void WhenSerializingCollection_ThenReturnsJsonArray()
    {
        //arrange
        var serializer = new BasicJsonSerializer();
        var list = new[] { "a", "b", "c" };

        //act
        var result = serializer.Serialize(list);

        //assert
        result.Should().Be("[\"a\",\"b\",\"c\"]");
    }

    private record TestDto(string Name, int Value);
}
