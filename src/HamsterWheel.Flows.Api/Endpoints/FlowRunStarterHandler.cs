using HamsterWheel.Flows.Auth;
using FlowNameType = HamsterWheel.Flows.FlowName;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// Starts flow runs for an endpoint through <see cref="IFlowRunStarter"/>, as the user of the current
/// request — <see cref="IFlowUserService"/> is the interface the host implements over whatever its auth
/// is — and waits inline for the result up to <see cref="RunTimeout"/>.
/// </summary>
/// <remarks>
/// Mapping the outcome is left to the derived handler, which knows the output type of its flow.
/// </remarks>
public abstract class FlowRunStarterHandler(IFlowRunStarter flowRunStarter, IFlowUserService currentUser)
{
    /// <summary>Name of the flow to run.</summary>
    public abstract string FlowName { get; }

    /// <summary>
    /// How long the run may take its result before the handler answers as pending. Hosts override it
    /// in their own handler when their budget differs from the package default.
    /// </summary>
    protected virtual TimeSpan RunTimeout => FlowsApiExtensions.DefaultRunTimeout;

    /// <summary>
    /// Starts <see cref="FlowName"/> with <paramref name="input"/> and waits for the outcome.
    /// </summary>
    protected Task<FlowRunOutcome> RunAsync(object? input, CancellationToken token)
        => flowRunStarter.RunAsync(FlowNameType.FromString(FlowName), currentUser.Id, input, RunTimeout, token);
}
