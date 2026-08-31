using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.DI;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class CopyFilesBlock : MultiplierPipelineBlock<CopyFilesInput, string, CopyFilesInputTaskSource>, INeedServices
{
    private const string AnyFilePattern = "*";

    private IFileSystem FileSystem
    {
        get => field ?? throw new BlockServicesNotInitializedException<CopyFilesBlock>(Id);
        set;
    }

    public void Resolve(IServiceProvider services) =>
        FileSystem = services.GetRequiredService<IFileSystem>();

    public override async Task RunForInput(CopyFilesInput input,
        Action<string> pushOutput, CancellationToken token)
    {
        var source = input.SourcePath;
        var target = input.TargetPath;
        var filter = input.Filter;
        var recursive = input.Recursive;
        await CopyFiles(source, target, filter, recursive, pushOutput);
    }

    private async Task CopyFiles(string source, string target,
        string? filter, bool recursive,
        Action<string> pushOutput)
    {
        Log($"Copying files from '{source}' to '{target}'...");
        var files = FileSystem.GetFiles(source, filter ?? AnyFilePattern);
        Log($"Creating target directory '{target}'...");
        FileSystem.CreateDirectory(target);
        foreach (var file in files)
        {
            Log($"Copying file '{file}'...");
            var destFileName = Path.Combine(target, Path.GetFileName(file));
            FileSystem.CopyFile(file, destFileName, true);
            pushOutput(destFileName);
        }

        if (recursive)
        {
            var dirs = FileSystem.EnumerateDirectories(source);
            foreach (var dir in dirs)
            {
                Log($"Checking nested files in '{dir}'...");
                await CopyFiles(dir, Path.Combine(target, Path.GetFileName(dir)), filter, recursive, pushOutput);
            }
        }
    }
}
