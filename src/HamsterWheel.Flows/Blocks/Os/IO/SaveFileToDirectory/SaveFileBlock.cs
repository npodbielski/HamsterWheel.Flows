using System.Text;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.DI;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class SaveFileBlock : NoOutPipelineBlock<SaveFileInput, SaveFileInputTaskSource>, INeedServices
{
    private IFileSystem FileSystem
    {
        get => field ?? throw new BlockServicesNotInitializedException<SaveFileBlock>(Id);
        set;
    }

    public void Resolve(IServiceProvider services) =>
        FileSystem = services.GetRequiredService<IFileSystem>();

    public override async Task RunForInput(SaveFileInput input, CancellationToken token)
    {
        var fileName = input.FileName;
        if (input.FileName.StartsWith(Path.DirectorySeparatorChar))
        {
            fileName = fileName[1..];
        }

        var path = Path.Combine(input.Directory, fileName);
        Log($"Opening file: {path}...");
        var directory = Path.GetDirectoryName(path);
        if (directory is not null && !FileSystem.DirectoryExists(directory))
        {
            Log($"creating directory: {directory}...");
            FileSystem.CreateDirectory(directory);
        }

        await using var file = FileSystem.OpenFile(path, FileMode.Create);
        Log($"Writing file: {path}...");
        await file.WriteAsync(Encoding.UTF8.GetBytes(input.Contents), token);
        Log("Done.");
    }
}
