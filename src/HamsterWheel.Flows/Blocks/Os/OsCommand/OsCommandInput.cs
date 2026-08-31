using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Blocks.Os;

public sealed class OsCommandInput
{
    private readonly Func<string, string>? _maskingFunction;

    public OsCommandInput(string command, string workingDir, string? initialArguments = null,
        Func<string, string>? maskingFunction = null)
    {
        _maskingFunction = maskingFunction;
        Command = command;
        Arguments = initialArguments;
        WorkingDir = workingDir;
        Validate();
    }

    public string Command { get; init; }
    public string? Arguments { get; init; }
    public string WorkingDir { get; init; }
    public string CommandWithArguments => $"{Command} {GetArgumentsWithMask()}";
    public int? Retries { get; set; }
    public int WaitForSeconds { get; init; } = 30;

    private string GetArgumentsWithMask()
    {
        var masked = _maskingFunction is not null && Arguments is not null ? _maskingFunction(Arguments) : Arguments;
        return string.Join(" ", masked);
    }

    private void Validate()
    {
        if (Command.IsNullOrWhiteSpace())
        {
            throw new InvalidOsCommandException<OsCommandInput>($"{nameof(Command)} cannot be null or whitespace.");
        }

        if (WorkingDir.IsNullOrWhiteSpace())
        {
            throw new InvalidOsCommandException<OsCommandInput>($"{nameof(WorkingDir)} cannot be null or whitespace.");
        }

        if (!Directory.Exists(WorkingDir))
        {
            throw new InvalidOsCommandException<OsCommandInput>($"{nameof(WorkingDir)} is not correct directory.");
        }
    }
}