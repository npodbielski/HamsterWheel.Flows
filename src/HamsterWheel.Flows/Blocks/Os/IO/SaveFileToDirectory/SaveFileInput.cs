namespace HamsterWheel.Flows.Blocks.Os.IO;

public class SaveFileInput(string fileName, string contents, string directory)
{
    public string FileName { get; } = fileName;
    public string Contents { get; } = contents;
    public string Directory { get; } = directory;
}