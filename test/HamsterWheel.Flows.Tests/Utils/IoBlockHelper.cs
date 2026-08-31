using HamsterWheel.Flows.Tests.Utils;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.Services.IO;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Tests.Utils;

public static class IoBlockHelper
{
    /// <summary>
    /// Resolves IFileSystem dependency on an INeedServices block using DefaultFileSystem.
    /// </summary>
    public static void ResolveIo(this INeedServices block)
    {
        var services = new ServiceCollection()
            .AddSingleton<IFileSystem, DefaultFileSystem>()
            .BuildServiceProvider();
        block.Resolve(services);
    }
}
