using System.Diagnostics.CodeAnalysis;
using HamsterWheel.Flows.Blocks.Os.IO;

namespace HamsterWheel.Flows.Services.IO;

/// <summary>
/// Default implementation using System.IO static methods.
/// </summary>
[ExcludeFromCodeCoverage]
public class DefaultFileSystem : IFileSystem
{
    public string[] GetFiles(string directory, string searchPattern) =>
        Directory.GetFiles(directory, searchPattern);

    public void CreateDirectory(string path) =>
        Directory.CreateDirectory(path);

    public void CopyFile(string source, string destination, bool overwrite) =>
        File.Copy(source, destination, overwrite);

    public IEnumerable<string> EnumerateDirectories(string path) =>
        Directory.EnumerateDirectories(path);

    public bool DirectoryExists(string path) =>
        Directory.Exists(path);

    public void DeleteDirectory(string path, bool recursive) =>
        Directory.Delete(path, recursive);

    public Stream OpenFile(string path, FileMode mode) =>
        File.Open(path, mode);

    public bool PathExists(string path) =>
        Path.Exists(path);
}
