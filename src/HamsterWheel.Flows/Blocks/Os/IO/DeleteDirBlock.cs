using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.IO;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class DeleteDirBlock : NoOutPipelineBlock<string, SingleInputTaskSource<string>>, INeedServices
{
    private IFileSystem FileSystem
    {
        get => field ?? throw new BlockServicesNotInitializedException<DeleteDirBlock>(Id);
        set;
    }

    public void Resolve(IServiceProvider services) =>
        FileSystem = services.GetRequiredService<IFileSystem>();

    public override Task RunForInput(string path, CancellationToken token)
    {
        Log("Checking if directory exists...");
        if (FileSystem.PathExists(path))
        {
            Log("Directory exists.");
            Log($"Deleting directory: {path}...");
            FileSystem.DeleteDirectory(path, true);
            Log($"Directory {path} removed.");
        }
        else
        {
            Log("Directory does not exist.");
        }

        return Task.CompletedTask;
    }
}
