namespace HamsterWheel.Flows;

public abstract class BlockException(string message, Exception? exception = null) : FlowException(message, exception);

public class BlockTriggerException(string id, Exception exception)
    : BlockException($"One of the block ({id}) triggers thrown an exception", exception);

public class BlockConditionException(string id, Exception exception)
    : BlockException($"Block ({id}) condition thrown an exception", exception);

public class BlockOperationException(string id, Exception exception)
    : BlockException($"Block ({id}) thrown an exception during its operation", exception);


public class ConstTaskSourceNotSetException() : BlockException("Cannot fetch constant value of input if it was set from task");
public class CannotFetchTaskSourceAsSingleException() : BlockException("Cannot fetch single value of task source if it was set to multiple");

public class BlockServicesNotInitializedException<TBlock>(string blockId)
    : BlockException($"Services for block: '{blockId}' of type: '{typeof(TBlock).Name}' were not initialized correctly");