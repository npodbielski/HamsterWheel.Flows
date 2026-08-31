namespace HamsterWheel.Flows.Blocks.Utils;

public class IncorrectMapOperationValueException(MapOperation type)
    : FlowException($"No mapping operation defined: '{type}'");