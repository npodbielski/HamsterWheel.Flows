using System.Diagnostics;

namespace HamsterWheel.Flows;

public static class ProcessExtensions
{
    extension(Process process)
    {
        public int? TryReadExitCode()
        {
            int? exitCode = null;
            try
            {
                exitCode = process.ExitCode;
            }
            catch (InvalidOperationException)
            {
                //swallow exception that process exit code cannot be read
            }

            return exitCode;
        }

        public async Task<Task> ReadAndWait(Action<string> logOutput,
            CancellationToken token, int numberOfSeconds = 30)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException("Operation cancelled.");
            }
            var delayIndex = 0;
            while (delayIndex++ < numberOfSeconds && !token.IsCancellationRequested)
            {
                //periodically check if process exited with errors
                var delay = Task.Delay(TimeSpan.FromSeconds(1), token);
                var exitTask = process.WaitForExitAsync(token);
                var task = await Task.WhenAny(delay, exitTask);
                //cancellation wins over a concurrent process exit
                if (token.IsCancellationRequested)
                {
                    throw new TaskCanceledException("Operation cancelled.");
                }
                if (task != delay && exitTask.IsCompleted)
                {
                    return exitTask;
                }
                else
                {
                    //reading output from process makes sure that process.Exited event was triggered
                    logOutput(await process.StandardOutput.ReadToEndAsync(token));
                    logOutput(await process.StandardError.ReadToEndAsync(token));
                }
            }

            throw new TimeoutException("Process does not exited before timeout.");
        }
    }
}
