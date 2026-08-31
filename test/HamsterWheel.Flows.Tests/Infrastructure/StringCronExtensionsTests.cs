using FluentAssertions;
using HamsterWheel.Flows;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class StringCronExtensionsTests
{
    [Theory]
    [InlineData("* * * * *")]
    [InlineData("0 12 * * *")]
    [InlineData("*/5 * * * *")]
    [InlineData("0 0 1 Jan *")]
    public void WhenStringIsCronExpression_ThenReturnsTrue(string cron)
    {
        //arrange
        // (none needed)

        //act
        var result = cron.IsCronExpression();

        //assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a cron")]
    [InlineData("12345")]
    [InlineData("*/invalid * * * *")]
    public void WhenStringIsNotCronExpression_ThenReturnsFalse(string cron)
    {
        //arrange
        // (none needed)

        //act
        var result = cron.IsCronExpression();

        //assert
        result.Should().BeFalse();
    }

    [Fact]
    public void WhenStringIsNull_ThenReturnsFalse()
    {
        //arrange
        string? cron = null;

        //act
        var result = cron.IsCronExpression();

        //assert
        result.Should().BeFalse();
    }
}
