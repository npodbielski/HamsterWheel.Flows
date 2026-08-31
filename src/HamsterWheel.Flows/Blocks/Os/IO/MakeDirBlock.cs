using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.IO;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class MakeDirBlock : PipelineBlock<string, string, SingleInputTaskSource<string>>, INeedServices
{
    private IFileSystem FileSystem
    {
        get => field ?? throw new BlockServicesNotInitializedException<MakeDirBlock>(Id);
        set;
    }

    public void Resolve(IServiceProvider services) =>
        FileSystem = services.GetRequiredService<IFileSystem>();

    public override Task<string> RunForInput(string path, CancellationToken token)
    {
        Log($"Creating directory: {path}...");
        FileSystem.CreateDirectory(path);
        Log($"Directory {path} created.");
        return Task.FromResult(path);
    }
}
