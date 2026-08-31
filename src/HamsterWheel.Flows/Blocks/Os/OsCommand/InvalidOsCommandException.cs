namespace HamsterWheel.Flows.Blocks.Os;

public class InvalidOsCommandException<T>(string message) : FlowException(
    $"Command input of type '{typeof(T).FullName}' was not correct: {message}");