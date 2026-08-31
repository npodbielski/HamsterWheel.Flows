namespace HamsterWheel.Flows.Blocks.Os;

public class OsCommandException(string command) : FlowException($"Could not start command: {command}");