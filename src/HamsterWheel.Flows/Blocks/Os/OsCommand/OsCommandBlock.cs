using System.Diagnostics;
using HamsterWheel.Flows.Blocks.Base;

namespace HamsterWheel.Flows.Blocks.Os;

public class OsCommandBlock : PipelineBlock<OsCommandInput, bool, OsCommandInputTaskSource>
{
    public override async Task<bool> RunForInput(OsCommandInput input, CancellationToken token)
    {
        Process process;
        if (input.Retries is not null && input.Retries > 0)
        {
            var output = false;
            while (input.Retries > 0)
            {
                process = GetProcess(input, input.Retries == 1);
                output = await RetryProcess(input, process, input.Retries == 1, token) == true;
                input.Retries--;
            }

            return output;
        }

        process = GetProcess(input);
        return await RetryProcess(input, process, token: token) == true;
    }

    private async Task<bool?> RetryProcess(OsCommandInput input, Process process, bool propagateOutput = true, CancellationToken token = default)
    {
        try
        {
            StartProcess(input, process);

            var exitTask = await process.ReadAndWait(LogIfNotEmpty, token, input.WaitForSeconds);
            if (exitTask.IsCompletedSuccessfully)
            {
                Log("Done.");
            }
            else if (exitTask.IsFaulted)
            {
                Log("Process failed.");
            }

            if (propagateOutput)
            {
                return process.ExitCode == 0;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var exitCode = process.TryReadExitCode();
            Log(exitCode is not null
                ? $"Command exited with code: {process.ExitCode}."
                : "Command process does not exited.");
            Log(await process.StandardOutput.ReadToEndAsync(token));
            Log(await process.StandardError.ReadToEndAsync(token));
            if (propagateOutput)
            {
                throw;
            }
        }

        return null;
    }

    private void StartProcess(OsCommandInput input, Process process)
    {
        Log($"Running command: {input.CommandWithArguments}...");
        if (process.Start())
        {
            return;
        }

        Log("Process could not be started.");
        throw new OsCommandException(input.CommandWithArguments);
    }

    private Process GetProcess(OsCommandInput input, bool propagateOutput = true)
    {
        var process = new Process
        {
            StartInfo =
            {
                FileName = input.Command,
                Arguments = input.Arguments is not null ? string.Join(" ", input.Arguments) : "",
                WorkingDirectory = Path.GetFullPath(input.WorkingDir),
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Exited += (_, _) =>
        {
            Log("Command exited.");
            if (process.ExitCode > 0 && propagateOutput)
            {
                //TODO: check if this can be moved to the block instead i.e. by setting a flag and throwing in transform
                // throw new FlowException($"Process exited with code {process.ExitCode}");
            }
        };
        return process;
    }
}