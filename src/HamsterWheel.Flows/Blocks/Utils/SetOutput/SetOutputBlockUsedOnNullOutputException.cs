namespace HamsterWheel.Flows.Blocks.Utils;

public class SetOutputBlockUsedOnNullOutputException() : BlockException(
    $"{nameof(SetOutputBlock)} cannot be used inside flow that has no output or output is null." +
    $" Make sure that '{nameof(IFlow)}.{nameof(IFlow.HaveOutput)}' is set to true and '{nameof(IFlow)}.{nameof(IFlow.BuildOutput)}' method is returning meaningful value.");