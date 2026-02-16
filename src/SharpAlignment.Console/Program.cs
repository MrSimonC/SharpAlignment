using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace SharpAlignment;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Reorganises, sorts and cleans up the provided C# file.");

        var noSortMembersByAlphabetOption = new Option<bool>(
            name: "--no-sort-members-by-alphabet",
            description: "Disables sorting members by alphabet.",
            getDefaultValue: () => false
        );
        var sortMembersByAlphabetCaseSensitiveOption = new Option<bool>(
            name: "--sort-members-by-alphabet-case-sensitive",
            description: "Enables case-sensitive sorting for member identifiers.",
            getDefaultValue: () => false
        );
        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            description: "Reports what would change without modifying files.",
            getDefaultValue: () => false
        );
        var systemUsingFirstOption = new Option<bool>(
            name: "--system-using-first",
            description: "Places using directives starting with 'System' before other usings.",
            getDefaultValue: () => false
        );

        var inputFileArgMeta = new
        {
            Name = "input",
            Description = "Path to input file or piped input."
        };

        var inputFileArg = new Argument<FileSystemInfo?>(
            name: "input",
            description: "Path to input file or directory, or piped input.",
            isDefault: true,
            parse: result =>
            {
                if (result.Tokens.Count != 0)
                {
                    var inputPath = result.Tokens[0].Value;
                    if (File.Exists(inputPath))
                    {
                        return new FileInfo(inputPath);
                    }
                    else if (Directory.Exists(inputPath))
                    {
                        return new DirectoryInfo(inputPath);
                    }

                    result.ErrorMessage = $"File or directory {inputPath} does not exist.";
                    return null;
                }
                else if (Console.IsInputRedirected)
                {
                    return null;
                }
                else
                {
                    result.ErrorMessage = "Missing file path or piped input.";
                    return null;
                }
            }
        );

        rootCommand.AddOption(noSortMembersByAlphabetOption);
        rootCommand.AddOption(sortMembersByAlphabetCaseSensitiveOption);
        rootCommand.AddOption(dryRunOption);
        rootCommand.AddOption(systemUsingFirstOption);
        rootCommand.AddArgument(inputFileArg);

        rootCommand.SetHandler(
            RootCommandHandler.Handle,
            new RootCommandConfigurationBinder(
                noSortMembersByAlphabetOption,
                sortMembersByAlphabetCaseSensitiveOption,
                dryRunOption,
                systemUsingFirstOption,
                inputFileArg
            )
        );

        var exitCode = await rootCommand.InvokeAsync(args);
        return exitCode;
    }
}