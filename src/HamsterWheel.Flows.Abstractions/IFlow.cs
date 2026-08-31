using HamsterWheel.Flows.Pipelines;
#if !NETSTANDARD2_0
using System.Text.Json.Serialization;
#endif

namespace HamsterWheel.Flows;

public interface IFlow
{
    /// <summary>
    /// Allows to set expected average run time for this flow. If actual run time will be much longer system will cancel this flow.
    /// </summary>
    public TimeSpan? AverageTime { get; }
    string Name { get; }

    public bool DoesAllowAnonymousRuns { get; }
    public string Definition { get; }
    public string? InputSchema { get; }
    public string? OutputSchema { get; }
    bool HaveInput { get; }
    bool HaveOutput { get; }
    string Version { get; }

#if !NETSTANDARD2_0
    [JsonIgnore]
    Type? InputType { get; }
    [JsonIgnore]
    Type? OutputType { get; }
#endif

    object? BuildOutput();
    Task ApplyToPipeline(IPipeline pipeline, IFlowGlobalInputBag resolver);
}
