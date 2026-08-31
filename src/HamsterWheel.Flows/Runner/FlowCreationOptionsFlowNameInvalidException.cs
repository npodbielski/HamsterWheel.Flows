namespace HamsterWheel.Flows.Runner;

public class FlowCreationOptionsFlowNameInvalidException(string? flowName)
    : FlowException($"Flow name is invalid: '{flowName}'");