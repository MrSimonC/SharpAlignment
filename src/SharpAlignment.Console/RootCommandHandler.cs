using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpAlignment.Padding;
using SharpAlignment.Reorganizing;
using SharpAlignment.Syntax;

namespace SharpAlignment;

public class RootCommandHandler
{
    private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static async Task<int> Handle(RootCommandConfiguration configuration)
    {
        if (configuration.Mode == InputOutputMode.Directory)
        {
            return await HandleDirectory(configuration).ConfigureAwait(false);
        }

        if (configuration.Mode == InputOutputMode.File && IsExcludedFile(configuration))
        {
            if (configuration.DryRun)
            {
                configuration.Console.WriteLine("no changes");
            }

            return 0;
        }

        var input = await ReadInput(configuration).ConfigureAwait(false);

        var clean = Clean(input);
        var root = Parse(clean);
        var organizedRoot = Reorganize(root, configuration);
        var output = organizedRoot.ToFullString();

        if (configuration.Mode == InputOutputMode.File && configuration.DryRun)
        {
            if (input == output)
            {
                configuration.Console.WriteLine("no changes");
                return 0;
            }

            configuration.Console.WriteLine(GetRequiredFilePath(configuration));
            return 1;
        }

        await WriteOutput(output, configuration).ConfigureAwait(false);
        return 0;
    }

    private static string Clean(string input)
    {
        var paddingCleaner = new PaddingCleaner();
        return paddingCleaner.Clean(input);
    }

    private static string GetRequiredFilePath(RootCommandConfiguration configuration)
    {
        if (configuration.File == null)
        {
            throw new InvalidOperationException("File mode requires a file input.");
        }

        return configuration.File.FullName;
    }

    private static async Task<int> HandleDirectory(RootCommandConfiguration configuration)
    {
        var directory = configuration.Directory;
        if (directory == null)
        {
            throw new InvalidOperationException("Directory mode requires a directory input.");
        }

        var excludedPaths = GetExcludedPaths(configuration.ExcludedPaths, directory.FullName);

        var files = directory
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBinOrObjPath(f.FullName))
            .Where(f => !IsExcludedPath(f.FullName, excludedPaths))
            .OrderBy(f => f.FullName, StringComparer.Ordinal)
            .ToList();

        var tasks = files.Select(file => Task.Run(async () =>
        {
            var input = await File.ReadAllTextAsync(file.FullName).ConfigureAwait(false);
            var clean = Clean(input);
            var root = Parse(clean);
            var organizedRoot = Reorganize(root, configuration);
            return (File: file, Input: input, Output: organizedRoot.ToFullString());
        }));

        var processedFiles = (await Task.WhenAll(tasks).ConfigureAwait(false))
            .OrderBy(r => r.File.FullName, StringComparer.Ordinal)
            .ToList();

        if (configuration.DryRun)
        {
            var changedFiles = processedFiles.FindAll(processedFile =>
                processedFile.Input != processedFile.Output
            );
            if (changedFiles.Count == 0)
            {
                configuration.Console.WriteLine("all files ok");
                return 0;
            }

            foreach (var (file, _, _) in changedFiles)
            {
                configuration.Console.WriteLine(file.FullName);
            }

            return 1;
        }

        foreach (var (file, _, output) in processedFiles)
        {
            await File.WriteAllTextAsync(file.FullName, output).ConfigureAwait(false);
        }

        return 0;
    }

    private static CompilationUnitSyntax Parse(string input)
    {
        var paddingCleaner = new PaddingCleaner();
        var cleanInput = paddingCleaner.Clean(input);
        var syntaxTree = CSharpSyntaxTree.ParseText(cleanInput);
        var root = syntaxTree.GetCompilationUnitRoot();
        return root;
    }

    private static async Task<string> ReadInput(RootCommandConfiguration configuration)
    {
        switch (configuration.Mode)
        {
            case InputOutputMode.Console:
                using (
                    var reader = new StreamReader(
                        Console.OpenStandardInput(),
                        Console.InputEncoding
                    )
                )
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }

            case InputOutputMode.File:
                return await File.ReadAllTextAsync(GetRequiredFilePath(configuration))
                    .ConfigureAwait(false);

            default:
                throw new NotImplementedException(
                    $"Mode \"{configuration.Mode}\" not implemented."
                );
        }
    }

    private static CompilationUnitSyntax Reorganize(
        CompilationUnitSyntax compilationUnit,
        RootCommandConfiguration configuration
    )
    {
        var comparer = new MemberInfoComparer(
            new MemberSortConfiguration
            {
                SortByAlphabet = configuration.SortMembersByAlphabet,
                SortByAlphabetCaseSensitive = configuration.SortMembersByAlphabetCaseSensitive,
            }
        );
        var usingComparer = new UsingInfoComparer(configuration.SystemUsingFirst);

        var organizer = new SyntaxReorganizerRewriter(comparer, usingComparer);
        return compilationUnit.Accept(organizer) as CompilationUnitSyntax
            ?? throw new Exception(
                $"Reorganized root is null or not of type {typeof(CompilationUnitSyntax)}."
            );
    }

    private static async Task WriteOutput(string output, RootCommandConfiguration configuration)
    {
        switch (configuration.Mode)
        {
            case InputOutputMode.Console:
                configuration.Console.Write(output);
                break;

            case InputOutputMode.File:
                await File.WriteAllTextAsync(GetRequiredFilePath(configuration), output)
                    .ConfigureAwait(false);

                break;

            default:
                throw new NotImplementedException(
                    $"Mode \"{configuration.Mode}\" not implemented."
                );
        }
    }

    private static bool IsBinOrObjPath(string filePath)
    {
        return filePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsExcludedFile(RootCommandConfiguration configuration)
    {
        var filePath = configuration.File?.FullName;
        if (filePath == null)
        {
            return false;
        }

        var excludedPaths = GetExcludedPaths(
            configuration.ExcludedPaths,
            Directory.GetCurrentDirectory()
        );
        return IsExcludedPath(filePath, excludedPaths);
    }

    private static bool IsExcludedPath(string filePath, IReadOnlyCollection<ExcludedPath> excludedPaths)
    {
        var normalizedFilePath = NormalizePath(filePath);

        foreach (var excludedPath in excludedPaths)
        {
            if (!excludedPath.IsDirectory)
            {
                if (string.Equals(normalizedFilePath, excludedPath.FullPath, _pathComparison))
                {
                    return true;
                }

                continue;
            }

            if (
                string.Equals(normalizedFilePath, excludedPath.FullPath, _pathComparison)
                || normalizedFilePath.StartsWith(excludedPath.DirectoryPrefix, _pathComparison)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyCollection<ExcludedPath> GetExcludedPaths(
        IEnumerable<string> excludedPaths,
        string baseDirectory
    )
    {
        var result = new List<ExcludedPath>();
        var seen = new HashSet<string>(_pathComparison == StringComparison.OrdinalIgnoreCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal);

        foreach (var rawPath in excludedPaths)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var resolvedPath = Path.IsPathRooted(rawPath)
                ? rawPath
                : Path.Combine(baseDirectory, rawPath);
            var normalizedPath = NormalizePath(resolvedPath);
            if (!seen.Add(normalizedPath))
            {
                continue;
            }

            var isDirectory = rawPath.EndsWith(Path.DirectorySeparatorChar)
                || rawPath.EndsWith(Path.AltDirectorySeparatorChar)
                || Directory.Exists(normalizedPath);

            result.Add(new ExcludedPath(normalizedPath, isDirectory));
        }

        return result;
    }

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(root))
        {
            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        if (fullPath.Length <= root.Length)
        {
            return fullPath;
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private readonly record struct ExcludedPath(string FullPath, bool IsDirectory)
    {
        public string DirectoryPrefix => FullPath + Path.DirectorySeparatorChar;
    }
}