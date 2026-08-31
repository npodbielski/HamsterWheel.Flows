using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;

namespace HamsterWheel.Flows;

public class FlowCancelledException(IEnumerable<IPipelineBlock> blocks) : FlowException(
    $"Pipeline was cancelled. Blocks that does not exited in time were:\r\n    - {string.Join("\r\n    - ", blocks.Select(b => b.Id))}");

public class AmbiguousFlowNameException(string name, string[] extensions) : FlowException(
    $"Flow with name: '{name}' is available in following extensions: {string.Join(", ", extensions)} you must specify which flow was targeted with extension prefix.");

public class FlowNotFoundException(IFlowName name) : FlowException($"Flow with name: '{name}' was not found");

public class FlowNoInputException(string name) : FlowException($"Flow with name: '{name}' expects user input");

public class MismatchedFlowOutputTypeException<TExpected>(string flowName, Type? actualType)
    : FlowException(
        $"Result of '{flowName}' flow should be of type: '{typeof(TExpected).Name}' but was " + (actualType is not null
            ? $"'{actualType.Name}'"
            : "missing."));

public class UserRequiredToRunFlowException(IFlow flow)
    : FlowException($"For flow: '{flow.Name}' user needs to be provided.");

public class MismatchedFlowInputTypeException(string flowName, Type? inputType)
    : FlowException($"Input of type: '{inputType?.Name ?? "missing"}' is not supported for flow: '{flowName}'.");

public class DuringFlowAttachMissingUserException()
    : FlowException($"{nameof(IFlowUserService)} should not be null during attaching flow to pipeline");

public class InsufficientPermissionsToCreateBlockException<T>(Guid userId)
    : FlowException($"User with Id: '{userId}' does not have permission to use block: '{typeof(T).Name}'")
    where T : IPipelineBlock;