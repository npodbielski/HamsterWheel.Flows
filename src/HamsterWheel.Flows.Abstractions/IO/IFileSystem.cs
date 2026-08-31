namespace HamsterWheel.Flows.Blocks.Os.IO;

/// <summary>
/// Thin wrapper around static File/Directory/Path operations for testability.
/// </summary>
public interface IFileSystem
{
    string[] GetFiles(string directory, string searchPattern);
    void CreateDirectory(string path);
    void CopyFile(string source, string destination, bool overwrite);
    IEnumerable<string> EnumerateDirectories(string path);
    bool DirectoryExists(string path);
    void DeleteDirectory(string path, bool recursive);
    Stream OpenFile(string path, FileMode mode);
    bool PathExists(string path);
}
