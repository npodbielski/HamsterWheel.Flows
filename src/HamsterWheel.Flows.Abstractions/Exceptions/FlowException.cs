namespace HamsterWheel.Flows;

public abstract class FlowException(string message, Exception? exception = null)
    : Exception(message, exception);
