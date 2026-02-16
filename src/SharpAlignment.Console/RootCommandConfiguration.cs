using System.CommandLine;
using System.IO;

namespace SharpAlignment;

public class RootCommandConfiguration
{
    public IConsole Console { get; init; } = null!;
    public DirectoryInfo? Directory { get; init; }
    public bool DryRun { get; init; }
    public FileInfo? File { get; init; }
    public InputOutputMode Mode { get; init; }
    public bool SortMembersByAlphabet { get; init; }
    public bool SortMembersByAlphabetCaseSensitive { get; init; }
    public bool SystemUsingFirst { get; init; }
}