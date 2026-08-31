using System.Diagnostics.CodeAnalysis;
using HamsterWheel.Flows.IO;
using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Runner;

public class FlowCreationOptions : IFlowCreationOptions, IDataWithValidator
{
    public IFlowName? FlowName { get; set; }
    public object? Input { get; set; }

    [MemberNotNull(nameof(FlowName))]
    public void Validate()
    {
        if (FlowName is null || FlowName.Name.IsNullOrWhiteSpace())
        {
            throw new FlowCreationOptionsFlowNameInvalidException(FlowName?.ToString());
        }
    }
}