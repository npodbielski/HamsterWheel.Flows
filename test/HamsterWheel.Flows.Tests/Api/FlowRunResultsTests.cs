using System.Text.Json;
using FluentAssertions;
using HamsterWheel.Flows.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Tests.Api;

/// <summary>
/// Result mapping of flow-run requests: status codes, pending flavors, output-type guard
/// </summary>
public class FlowRunResultsTests
{
    private const string FlowNameValue = "SomeFlow";
    private static readonly FlowName FlowName = new(FlowNameValue);
    private static readonly Guid RunId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void WhenSuccess_ThenReturns200WithOutput()
    {
        //arrange
        var outcome = Succeeded(new SomeOutput("value"));

        //act
        var result = FlowRunResults.Map(outcome);

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status200OK);
        Body(result).GetProperty("value").GetString().Should().Be("value");
    }

    [Fact]
    public void WhenSuccessWithCustomSuccessMapping_ThenItIsUsed()
    {
        //arrange
        var outcome = Succeeded(new SomeOutput("value"));

        //act
        var result = FlowRunResults.Map(outcome, _ => Results.NoContent());


        //assert
        StatusCode(result).Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void WhenFlowNotFound_ThenReturns404WithFlowName()
    {
        //arrange
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.Failed, null,
            new FlowNotFoundException(FlowName), RunId);

        //act
        var result = FlowRunResults.Map(outcome);

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status404NotFound);
        Body(result).GetProperty("flowName").GetString().Should().Be(FlowNameValue);
    }

    [Fact]
    public void WhenRunFailed_ThenReturns500WithError()
    {
        //arrange
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.Failed, null,
            new InvalidOperationException("flow exploded"), RunId);

        //act
        var result = FlowRunResults.Map(outcome);

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status500InternalServerError);
        Body(result).GetProperty("detail").GetString().Should().Contain("flow exploded");
    }

    [Fact]
    public void WhenPendingWithoutRunResource_ThenReturns202WithRunId()
    {
        //arrange
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.TimedOut, null, null, RunId);

        //act
        var result = FlowRunResults.Map(outcome);

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status202Accepted);
        Body(result).GetProperty("runId").GetGuid().Should().Be(RunId);
        Body(result).GetProperty("status").GetString().Should().Be("Running");
    }

    [Fact]
    public async Task WhenPendingWithCustomMapping_ThenItIsUsed()
    {
        //arrange
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.TimedOut, null, null, RunId);

        //act
        var result = FlowRunResults.Map(outcome, timedOut: o => Results.Created($"/runs/{o.RunId:D}", o.RunId));

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status201Created);
        (await LocationAsync(result)).Should().Be($"/runs/{RunId:D}");
    }

    [Fact]
    public async Task WhenPendingWithRunResource_ThenReturns201WithRunLocation()
    {
        //arrange
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.TimedOut, null, null, RunId);

        //act
        var result = FlowRunResults.Pending(outcome, "/core/flows/{flowName}/run/{runId}");

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status201Created);
        (await LocationAsync(result)).Should().Be($"/core/flows/{FlowNameValue}/run/{RunId:D}");
        Body(result).GetProperty("status").GetString().Should().Be("Running");
    }

    [Fact]
    public void WhenPendingWithoutRunId_ThenFallsBackTo202()
    {
        //arrange (a starter that cannot report the run id cannot promise a location)
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.TimedOut, null, null);

        //act
        var result = FlowRunResults.Pending(outcome, "/core/flows/{flowName}/run/{runId}");

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public void WhenPendingWithEmptyRunId_ThenFallsBackTo202()
    {
        //arrange
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.TimedOut, null, null, Guid.Empty);

        //act
        var result = FlowRunResults.Pending(outcome, "/core/flows/{flowName}/run/{runId}");

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status202Accepted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WhenNoRunResourceTemplate_ThenNoLocationProduced(string? template)
    {
        //arrange
        var outcome = new FlowRunOutcome(FlowName, FlowRunStatus.TimedOut, null, null, RunId);

        //act & assert
        FlowRunResults.RunLocation(template, outcome).Should().BeNull();
    }

    [Fact]
    public void WhenTypedOutputMatches_ThenReturns200WithOutput()
    {
        //arrange
        var outcome = Succeeded(new SomeOutput("value"));

        //act
        var result = FlowRunResults.Map<SomeOutput>(outcome);

        //assert
        StatusCode(result).Should().Be(StatusCodes.Status200OK);
        Body(result).GetProperty("value").GetString().Should().Be("value");
    }

    [Fact]
    public void WhenTypedOutputMissing_ThenThrowsMismatchedException()
    {
        //arrange
        var outcome = Succeeded(output: null);

        //act
        var act = () => FlowRunResults.Map<SomeOutput>(outcome);

        //assert
        act.Should().Throw<MismatchedFlowOutputTypeException<SomeOutput>>()
            .WithMessage($"*'{FlowNameValue}'*");
    }

    [Fact]
    public void WhenTypedOutputHasDifferentType_ThenThrowsMismatchedException()
    {
        //arrange
        var outcome = Succeeded(new OtherOutput());

        //act
        var act = () => FlowRunResults.Map<SomeOutput>(outcome);

        //assert
        act.Should().Throw<MismatchedFlowOutputTypeException<SomeOutput>>()
            .WithMessage($"*{nameof(SomeOutput)}*{nameof(OtherOutput)}*");
    }

    private static FlowRunOutcome Succeeded(object? output) =>
        new(FlowName, FlowRunStatus.Success, output, null, RunId);

    private static int StatusCode(IResult result) => ((IStatusCodeHttpResult)result).StatusCode ?? 0;

    //the run location is a response header, so the result has to be executed to read it
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

    //the result bodies are anonymous types internal to the package - read them through JSON
    //(camelCase, like the ASP.NET Core JSON writer the package's results use)
    private static readonly JsonSerializerOptions BodyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static JsonElement Body(IResult result) =>
        JsonSerializer.SerializeToElement(((IValueHttpResult)result).Value, BodyOptions);

    private sealed record SomeOutput(string Value);

    private sealed record OtherOutput;
}
