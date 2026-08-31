using FluentAssertions;
using HamsterWheel.Flows.Data.Validation;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class DataValidatorFactoryTests
{
    [Fact]
    public void WhenForIsCalled_ThenReturnsValidator()
    {
        //arrange
        var factory = new DataValidatorFactory();

        //act
        var validator = factory.For<TestDto>();

        //assert
        validator.Should().NotBeNull();
        validator.Should().BeAssignableTo<IDataValidator<TestDto>>();
    }

    [Fact]
    public void WhenForWithObjectIsCalled_ThenReturnsValidator()
    {
        //arrange
        var factory = new DataValidatorFactory();
        var obj = new TestDto { Name = "test" };

        //act
        var validator = factory.For(obj);

        //assert
        validator.Should().NotBeNull();
    }

    [Fact]
    public void WhenPropertyMustPasses_ThenNoException()
    {
        //arrange
        var factory = new DataValidatorFactory();
        var validator = factory.For<TestDto>();
        var obj = new TestDto { Name = "valid" };

        //act
        var action = () =>
        {
            validator.Property(x => x.Name).Must(x => !string.IsNullOrEmpty(x));
        };

        //assert
        action.Should().NotThrow();
    }

    [Fact]
    public void WhenMultiplePropertiesAreRegistered_ThenNoException()
    {
        //arrange
        var factory = new DataValidatorFactory();
        var validator = factory.For<TestDto>();

        //act
        var action = () =>
        {
            validator.Property(x => x.Name).Must(x => x != null);
            validator.Property(x => x.Age).Must(x => x > 0);
        };

        //assert
        action.Should().NotThrow();
    }

    private class TestDto
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
