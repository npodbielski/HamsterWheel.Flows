using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api;

/// <summary>
/// HTTP result mapping for flow-run requests.
///
/// The status code describes the outcome of the *request* (200 finished with output,
/// 404 unknown flow, 500 failed, pending); the flow run's own progress is a property of the
/// run resource, never of the status code. Pending maps to 202 Accepted unless the host exposes
/// a queryable run resource, in which case <see cref="Pending(FlowRunOutcome,string)"/> answers
/// 201 Created + Location for the run that was created.
/// </summary>
public static class FlowRunResults
{
    private const string PendingStatus = "Running";
    private const string FlowNamePlaceholder = "{flowName}";
    private const string RunIdPlaceholder = "{runId}";

    /// <summary>
    /// Maps a run outcome: success (optionally customized), unknown flow 404,
    /// failed 500, pending (202 by default, or <paramref name="timedOut"/>).
    /// </summary>
    public static IResult Map(
        FlowRunOutcome outcome,
        Func<FlowRunOutcome, IResult>? success = null,
        Func<FlowRunOutcome, IResult>? timedOut = null) =>
        MapCore(outcome, o => success?.Invoke(o) ?? Results.Ok(o.Output), timedOut);

    /// <summary>
    /// Maps a run outcome for an endpoint that knows the flow's output type: a successful run
    /// whose output is not <typeparamref name="TOutput"/> throws
    /// <see cref="MismatchedFlowOutputTypeException{TExpected}"/> instead of returning a
    /// mismatched payload.
    /// </summary>
    public static IResult Map<TOutput>(
        FlowRunOutcome outcome,
        Func<FlowRunOutcome, IResult>? timedOut = null) =>
        MapCore(outcome, Success<TOutput>, timedOut);

    /// <summary>
    /// Success of a run whose endpoint declares no output type: the output of the flow is returned
    /// when it has one, and 200 OK with no body when it has none - nothing was promised to the
    /// caller, so nothing is checked. Endpoints that do declare a type use <see cref="Map{TOutput}"/>.
    /// </summary>
    public static IResult OkOutput(FlowRunOutcome outcome) =>
        outcome.Output is { } output ? Results.Ok(output) : Results.Ok();

    /// <summary>
    /// 202 Accepted - the run reached no terminal result within the budget and the host makes no
    /// promise about a resource the caller can poll.
    /// </summary>
    public static IResult Accepted(FlowRunOutcome outcome) =>
        Results.Accepted(value: new
        {
            flowName = outcome.FlowName.ToString(),
            runId = outcome.RunId,
            status = PendingStatus,
            message =
                "Flow run has not reached a terminal result within the timeout budget and may still be running."
        });

    /// <summary>
    /// 201 Created + Location - the run resource was created and is dereferenceable at
    /// <paramref name="location"/>; the body reports it as still pending.
    /// </summary>
    public static IResult Created(FlowRunOutcome outcome, string location) =>
        Results.Created(location, value: new
        {
            flowName = outcome.FlowName.ToString(),
            runId = outcome.RunId,
            status = PendingStatus,
            location
        });

    /// <summary>
    /// Pending result of a run: 201 Created + Location when <paramref name="urlTemplate"/> yields
    /// a location for this run, otherwise 202 Accepted. Use this for hosts whose run resource is
    /// only queryable once the run is tracked (an untracked run has no location to point at).
    /// </summary>
    /// <remarks>
    /// <paramref name="urlTemplate"/> supports the {flowName} and {runId} placeholders.
    /// </remarks>
    public static IResult Pending(FlowRunOutcome outcome, string? urlTemplate = null) =>
        RunLocation(urlTemplate, outcome) is { } location
            ? Created(outcome, location)
            : Accepted(outcome);

    /// <summary>
    /// Fills the {flowName} / {runId} placeholders of <paramref name="urlTemplate"/>, or null when
    /// no location can be produced (no template, or the starter reported no run id).
    /// </summary>
    public static string? RunLocation(string? urlTemplate, FlowRunOutcome outcome)
    {
        if (string.IsNullOrEmpty(urlTemplate) || outcome.RunId is not { } runId || runId == Guid.Empty)
        {
            return null;
        }

        return urlTemplate
            .Replace(FlowNamePlaceholder, outcome.FlowName.ToString())
            .Replace(RunIdPlaceholder, runId.ToString("D"));
    }

    private static IResult Success<TOutput>(FlowRunOutcome outcome)
    {
        if (outcome.Output is null)
        {
            //a flow with no output cannot satisfy a typed endpoint
            throw new MismatchedFlowOutputTypeException<TOutput>(outcome.FlowName.ToString(), null);
        }

        if (!typeof(TOutput).IsAssignableFrom(outcome.Output.GetType()))
        {
            throw new MismatchedFlowOutputTypeException<TOutput>(outcome.FlowName.ToString(),
                outcome.Output.GetType());
        }

        return Results.Ok(outcome.Output);
    }

    private static IResult MapCore(
        FlowRunOutcome outcome,
        Func<FlowRunOutcome, IResult> success,
        Func<FlowRunOutcome, IResult>? timedOut)
    {
        switch (outcome.Status)
        {
            case FlowRunStatus.Success:
                return success(outcome);
            case FlowRunStatus.Failed when outcome.Error is FlowNotFoundException:
                return Results.NotFound(new
                {
                    error = "Flow not found",
                    flowName = outcome.FlowName.ToString()
                });
            case FlowRunStatus.Failed:
                return Results.Problem(
                    title: "Flow run failed",
                    detail: outcome.Error?.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            case FlowRunStatus.TimedOut:
                //no terminal result within the budget; the run itself may still be in progress
                return timedOut?.Invoke(outcome) ?? Accepted(outcome);
            default:
                return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

