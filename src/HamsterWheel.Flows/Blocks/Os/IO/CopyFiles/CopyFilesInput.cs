namespace HamsterWheel.Flows.Blocks.Os.IO;

public class CopyFilesInput
{
    /// <summary>
    /// Source path to copy files from
    /// </summary>
    public required string SourcePath { get; set; }
    /// <summary>
    /// Target path to copy files to
    /// </summary>
    public required string TargetPath { get; set; }
    /// <summary>
    /// If files in nested directories should be copied to, recursively
    /// </summary>
    public bool Recursive { get; set; }
    /// <summary>
    /// Filter if only some files should be copied
    /// </summary>
    public string? Filter { get; set; }
}