using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// Result-mapping part of a flow-run endpoint that the host generates or writes by hand: validation
/// of the input, and what a run that reached no terminal result within the budget is worth.
/// The outcome itself is always mapped by the package (<see cref="FlowRunResults"/>).
/// </summary>
/// <remarks>
/// Deliberately free of host concepts (routes, extension metadata, dependency resolution): a host
/// adds those in its own derived class, which is also where the run link is declared — see
/// <see cref="RunUrlTemplate"/>.
/// </remarks>
public abstract class FlowRunEndpointBase
{
    /// <summary>
    /// Template of the run resource a pending run points at, answered as 201 Created + Location
    /// (<see cref="FlowRunResults.Pending"/>). <c>null</c> — the default — means the host offers no
    /// resource the caller could query for this run, so the same case answers 202 Accepted.
    /// The template may use the <c>{flowName}</c> and <c>{runId}</c> placeholders.
    /// </summary>
    protected virtual string? RunUrlTemplate => null;

    /// <summary>
    /// The pending result of a run: 201 + Location when <see cref="RunUrlTemplate"/> yields one for
    /// this run, 202 Accepted otherwise. Handed to the handler, which owns the rest of the mapping.
    /// </summary>
    protected IResult PendingRun(FlowRunOutcome outcome) => FlowRunResults.Pending(outcome, RunUrlTemplate);

    /// <summary>
    /// Input validation before the run is started: no validator registered for
    /// <typeparamref name="TInput"/> means nothing to check.
    /// </summary>
    protected static Task ValidateAsync<TInput>(IValidator<TInput>? validator, TInput input, CancellationToken token) =>
        validator is null ? Task.CompletedTask : validator.ValidateAndThrowAsync(input, token);
}
