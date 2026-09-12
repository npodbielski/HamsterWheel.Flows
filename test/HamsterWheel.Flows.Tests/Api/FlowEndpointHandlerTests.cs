using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using HamsterWheel.Flows.Api;
using HamsterWheel.Flows.Api.Endpoints;
using HamsterWheel.Flows.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HamsterWheel.Flows.Tests.Api;

/// <summary>
/// Handlers and the endpoint base a host derives from: who the run is started as, what the outcome
/// is answered with, and what the host can still decide (pending result, validation, timeout).
/// </summary>
public class FlowEndpointHandlerTests
{
    private static readonly FlowName SomeFlow = new("SomeFlow");
    private static readonly Guid RunId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task WhenTypedHandlerRuns_ThenRunIsStartedAsTheFlowUserWithDefaultBudget()
    {
        //arrange
        var starter = new RecordingStarter(Succeeded(new SomeOutput("value")));

        //act
        var result = await new TypedHandler(starter, new FlowUser("user-1")).Start(new SomeInput("input"));

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status200OK);
        Body(result).GetProperty("value").GetString().Should().Be("value");
        starter.LastFlowName.Should().Be(SomeFlow);
        starter.LastUserId.Should().Be("user-1");
        starter.LastInput.Should().BeOfType<SomeInput>();
        starter.LastRunTimeout.Should().Be(FlowsApiExtensions.DefaultRunTimeout);
    }

    [Fact]
    public async Task WhenTypedHandlerOverridesBudget_ThenItIsPassedToTheStarter()
    {
        //arrange
        var starter = new RecordingStarter(Succeeded(new SomeOutput("value")));

        //act
        await new HandlerWithOwnBudget(starter, new FlowUser("user-1")).Start(new SomeInput("input"));

        //assert
        starter.LastRunTimeout.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task WhenRunTimedOut_ThenHandlerUsesThePendingResultOfTheEndpoint()
    {
        //arrange
        var starter = new RecordingStarter(new FlowRunOutcome(SomeFlow, FlowRunStatus.TimedOut, null, null, RunId));

        //act
        var result = await new TypedHandler(starter, new FlowUser("user-1"))
            .Start(new SomeInput("input"), _ => Results.NoContent());

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task WhenRunTimedOutWithoutPendingResult_ThenReturns202()
    {
        //arrange
        var starter = new RecordingStarter(new FlowRunOutcome(SomeFlow, FlowRunStatus.TimedOut, null, null, RunId));

        //act
        var result = await new TypedHandler(starter, new FlowUser("user-1")).Start(new SomeInput("input"));

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public async Task WhenRunOutputIsNotTheDeclaredOne_ThenThrowsMismatch()
    {
        //arrange
        var starter = new RecordingStarter(Succeeded(new OtherOutput()));

        //act
        var act = async () => await new TypedHandler(starter, new FlowUser("user-1")).Start(new SomeInput("input"));

        //assert
        await act.Should().ThrowAsync<MismatchedFlowOutputTypeException<SomeOutput>>();
    }

    [Fact]
    public async Task WhenRunFailed_ThenReturnsProblem()
    {
        //arrange
        var starter = new RecordingStarter(new FlowRunOutcome(SomeFlow, FlowRunStatus.Failed, null,
            new InvalidOperationException("boom"), RunId));

        //act
        var result = await new TypedHandler(starter, new FlowUser("user-1")).Start(new SomeInput("input"));

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task WhenFlowHasNoDeclaredOutput_ThenItsOutputIsStillReturned()
    {
        //arrange
        var starter = new RecordingStarter(Succeeded(new SomeOutput("value")));

        //act
        var result = await new NoOutputHandler(starter, new FlowUser("user-1")).Start(new SomeInput("input"));

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status200OK);
        Body(result).GetProperty("value").GetString().Should().Be("value");
    }

    [Fact]
    public async Task WhenFlowHasNoInput_ThenRunIsStartedWithNoInput()
    {
        //arrange
        var starter = new RecordingStarter(Succeeded(new SomeOutput("value")));

        //act
        var result = await new NoInputTypedHandler(starter, new FlowUser("user-1")).Start();

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status200OK);
        starter.LastInput.Should().BeNull();
        starter.LastFlowName.Should().Be(SomeFlow);
    }

    [Fact]
    public async Task WhenFlowHasNeitherInputNorOutput_ThenReturns200WithNoBody()
    {
        //arrange
        var starter = new RecordingStarter(Succeeded(null));

        //act
        var result = await new NoInputNoOutputHandler(starter, new FlowUser("user-1")).Start();

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeAssignableTo<IValueHttpResult>("the flow has no output to return");
    }

    [Fact]
    public void WhenEndpointHasNoRunTemplate_ThenPendingRunIsAccepted()
    {
        //arrange
        var endpoint = new TestEndpoint();

        //act
        var result = endpoint.AnswerPendingRun(TimedOut());

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public async Task WhenEndpointHasRunTemplate_ThenPendingRunIsCreatedWithLocation()
    {
        //arrange
        var endpoint = new TestEndpoint { Template = "/flows/runs/{runId}" };

        //act
        var result = endpoint.AnswerPendingRun(TimedOut());

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status201Created);
        (await LocationAsync(result)).Should().Be($"/flows/runs/{RunId:D}");
    }

    [Fact]
    public async Task WithoutValidator_ThenValidationDoesNothing()
    {
        //arrange & act
        var act = async () => await TestEndpoint.AnswerValidation(null, new SomeInput("input"));

        //assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WhenInputIsInvalid_ThenValidationThrows()
    {
        //arrange
        var validator = new FailingValidator();

        //act
        var act = async () => await TestEndpoint.AnswerValidation(validator, new SomeInput("input"));

        //assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    private static FlowRunOutcome Succeeded(object? output)
        => new(SomeFlow, FlowRunStatus.Success, output, null, RunId);

    private static FlowRunOutcome TimedOut()
        => new(SomeFlow, FlowRunStatus.TimedOut, null, null, RunId);

    private sealed record SomeInput(string Value);

    private sealed record SomeOutput(string Value);

    private sealed record OtherOutput;

    private sealed class FlowUser(string? id) : IFlowUserService
    {
        public string? Id => id;
        public string[] Permissions => [];
    }

    private sealed class RecordingStarter(FlowRunOutcome outcome) : IFlowRunStarter
    {
        public FlowName? LastFlowName { get; private set; }
        public string? LastUserId { get; private set; }
        public object? LastInput { get; private set; }
        public TimeSpan? LastRunTimeout { get; private set; }

        public Task<FlowRunOutcome> RunAsync(
            FlowName flowName, string? userId, object? input, TimeSpan runTimeout, CancellationToken token)
        {
            LastFlowName = flowName;
            LastUserId = userId;
            LastInput = input;
            LastRunTimeout = runTimeout;
            return Task.FromResult(outcome);
        }
    }

    private sealed class TypedHandler(IFlowRunStarter starter, IFlowUserService user)
        : FlowEndpointHandler<SomeInput, SomeOutput>(starter, user)
    {
        public override string FlowName => "SomeFlow";
    }

    private sealed class HandlerWithOwnBudget(IFlowRunStarter starter, IFlowUserService user)
        : FlowEndpointHandler<SomeInput, SomeOutput>(starter, user)
    {
        public override string FlowName => "SomeFlow";

        protected override TimeSpan RunTimeout => TimeSpan.FromSeconds(7);
    }

    private sealed class NoOutputHandler(IFlowRunStarter starter, IFlowUserService user)
        : NoOutputFlowEndpointHandler<SomeInput>(starter, user)
    {
        public override string FlowName => "SomeFlow";
    }

    private sealed class NoInputTypedHandler(IFlowRunStarter starter, IFlowUserService user)
        : NoInputFlowEndpointHandler<SomeOutput>(starter, user)
    {
        public override string FlowName => "SomeFlow";
    }

    private sealed class NoInputNoOutputHandler(IFlowRunStarter starter, IFlowUserService user)
        : NoInputNoOutputFlowEndpointHandler(starter, user)
    {
        public override string FlowName => "SomeFlow";
    }

    private sealed class TestEndpoint : FlowRunEndpointBase
    {
        public string? Template { get; init; }

        protected override string? RunUrlTemplate => Template;

        public IResult AnswerPendingRun(FlowRunOutcome outcome) => PendingRun(outcome);

        public static Task AnswerValidation(IValidator<SomeInput>? validator, SomeInput input)
            => ValidateAsync(validator, input, CancellationToken.None);
    }

    private sealed class FailingValidator : AbstractValidator<SomeInput>
    {
        public FailingValidator() => RuleFor(i => i.Value).NotEmpty().Equal("never");
    }

    private static int StatusCode(IResult result) => ((IStatusCodeHttpResult)result).StatusCode ?? 0;

    //the location is a response header, so the result has to be executed to read it
    private static async Task<string?> LocationAsync(IResult result)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
        await result.ExecuteAsync(context);
        return context.Response.Headers.Location.ToString();
    }

    private static readonly JsonSerializerOptions BodyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static JsonElement Body(IResult result) =>
        JsonSerializer.SerializeToElement(((IValueHttpResult)result).Value, BodyOptions);
}
