using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace SharpAlignment.IntegrationTests;

public class IntegrationTests
{
    private const string _testCasesDir = "TestCases";

    public static TheoryData<string> TestCases()
    {
        var testCases = Directory
            .EnumerateDirectories(_testCasesDir)
            .Select(dir => dir.Split(Path.DirectorySeparatorChar)[^1]);

        var data = new TheoryData<string>();
        foreach (var testCase in testCases)
        {
            data.Add(testCase);
        }

        return data;
    }

    [Fact]
    public async Task DirectoryModeSkipsBinAndObjFoldersWhenScanning()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var cleanPath = Path.Combine(_testCasesDir, folder, "clean.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var changedFilePath = Path.Combine(tempDir, "changed.cs");
        var binChangedFilePath = Path.Combine(tempDir, "bin", "changed.cs");
        var objChangedFilePath = Path.Combine(tempDir, "obj", "changed.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(binChangedFilePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(objChangedFilePath)!);
        File.Copy(originalPath, changedFilePath);
        File.Copy(originalPath, binChangedFilePath);
        File.Copy(originalPath, objChangedFilePath);

        try
        {
            var exitCode = await Program.Main([tempDir]);

            exitCode.Should().Be(0);
            (await File.ReadAllTextAsync(changedFilePath)).Should().Be(await File.ReadAllTextAsync(cleanPath));
            (await File.ReadAllTextAsync(binChangedFilePath)).Should().Be(await File.ReadAllTextAsync(originalPath));
            (await File.ReadAllTextAsync(objChangedFilePath)).Should().Be(await File.ReadAllTextAsync(originalPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task DirectUseOfProgramClass(string folder)
    {
        // Arrange
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var cleanPath = Path.Combine(_testCasesDir, folder, "clean.cs.test");

        var testPath = Path.Combine(_testCasesDir, folder, $"{Guid.NewGuid().ToString()[..7]}.cs");

        // Debug
        var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(cleanPath));
        var root = syntaxTree.GetCompilationUnitRoot();

        // Act
        File.Copy(originalPath, testPath);
        var exitCode = await Program.Main([testPath]);
        var result = await File.ReadAllTextAsync(testPath);
        File.Delete(testPath);

        // Assert
        var clean = await File.ReadAllTextAsync(cleanPath);
        exitCode.Should().Be(0);
        result.Should().Be(clean);
    }

    [Fact]
    public async Task DryRunDirectoryPrintsChangedFilesInStableOrder()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var secondChangedFilePath = Path.Combine(tempDir, "z-dir", "b.cs");
        var firstChangedFilePath = Path.Combine(tempDir, "a-dir", "a.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(secondChangedFilePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(firstChangedFilePath)!);
        File.Copy(originalPath, secondChangedFilePath);
        File.Copy(originalPath, firstChangedFilePath);

        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(["--dry-run", tempDir]);
            var lines = output
                .ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .ToArray();

            exitCode.Should().Be(1);
            lines.Should().Equal([firstChangedFilePath, secondChangedFilePath]);
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DryRunDirectoryScansRecursivelyAndPrintsChangedFilePathsAndReturnsOne()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var cleanPath = Path.Combine(_testCasesDir, folder, "clean.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var nestedDir = Path.Combine(tempDir, "nested", "deeper");
        Directory.CreateDirectory(nestedDir);
        var changedFilePath = Path.Combine(nestedDir, "changed.cs");
        var cleanFilePath = Path.Combine(nestedDir, "clean.cs");
        File.Copy(originalPath, changedFilePath);
        File.Copy(cleanPath, cleanFilePath);

        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(["--dry-run", tempDir]);

            exitCode.Should().Be(1);
            output.ToString().Should().Contain(changedFilePath);
            output.ToString().Should().NotContain(cleanFilePath);
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DryRunDirectorySkipsBinAndObjFoldersWhenScanning()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var changedFilePath = Path.Combine(tempDir, "changed.cs");
        var binChangedFilePath = Path.Combine(tempDir, "bin", "changed.cs");
        var objChangedFilePath = Path.Combine(tempDir, "obj", "changed.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(binChangedFilePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(objChangedFilePath)!);
        File.Copy(originalPath, changedFilePath);
        File.Copy(originalPath, binChangedFilePath);
        File.Copy(originalPath, objChangedFilePath);

        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(["--dry-run", tempDir]);
            var lines = output
                .ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .ToArray();

            exitCode.Should().Be(1);
            lines.Should().Equal(changedFilePath);
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DryRunDirectoryExcludesSpecifiedDirectory()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var excludedDir = Path.Combine(tempDir, "excluded");
        var includedDir = Path.Combine(tempDir, "included");
        Directory.CreateDirectory(excludedDir);
        Directory.CreateDirectory(includedDir);
        var excludedFilePath = Path.Combine(excludedDir, "excluded.cs");
        var includedFilePath = Path.Combine(includedDir, "included.cs");
        File.Copy(originalPath, excludedFilePath);
        File.Copy(originalPath, includedFilePath);

        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(["--dry-run", "--exclude", excludedDir, tempDir]);
            var lines = output
                .ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .ToArray();

            exitCode.Should().Be(1);
            lines.Should().Equal(includedFilePath);
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DryRunDirectoryExcludesMultiplePaths()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var excludedDir = Path.Combine(tempDir, "excluded");
        Directory.CreateDirectory(excludedDir);
        var excludedFilePath = Path.Combine(excludedDir, "excluded.cs");
        var secondExcludedFilePath = Path.Combine(tempDir, "also-excluded.cs");
        var includedFilePath = Path.Combine(tempDir, "included.cs");
        File.Copy(originalPath, excludedFilePath);
        File.Copy(originalPath, secondExcludedFilePath);
        File.Copy(originalPath, includedFilePath);

        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(
                [
                    "--dry-run",
                    "--exclude",
                    excludedDir,
                    "--exclude",
                    secondExcludedFilePath,
                    tempDir,
                ]
            );
            var lines = output
                .ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .ToArray();

            exitCode.Should().Be(1);
            lines.Should().Equal(includedFilePath);
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DirectoryModeExcludesSpecifiedDirectoryWhenWriting()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var cleanPath = Path.Combine(_testCasesDir, folder, "clean.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var excludedDir = Path.Combine(tempDir, "excluded");
        var includedDir = Path.Combine(tempDir, "included");
        Directory.CreateDirectory(excludedDir);
        Directory.CreateDirectory(includedDir);
        var excludedFilePath = Path.Combine(excludedDir, "excluded.cs");
        var includedFilePath = Path.Combine(includedDir, "included.cs");
        File.Copy(originalPath, excludedFilePath);
        File.Copy(originalPath, includedFilePath);

        try
        {
            var exitCode = await Program.Main(["--exclude", excludedDir, tempDir]);

            exitCode.Should().Be(0);
            (await File.ReadAllTextAsync(includedFilePath)).Should().Be(await File.ReadAllTextAsync(cleanPath));
            (await File.ReadAllTextAsync(excludedFilePath)).Should().Be(await File.ReadAllTextAsync(originalPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DryRunDirectoryWithNoChangesPrintsAllFilesOkAndReturnsZero()
    {
        var folder = (string)TestCases().First()[0];
        var cleanPath = Path.Combine(_testCasesDir, folder, "clean.cs.test");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var testFilePath = Path.Combine(tempDir, "clean.cs");
        File.Copy(cleanPath, testFilePath);

        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(["--dry-run", tempDir]);

            exitCode.Should().Be(0);
            output.ToString().Trim().Should().Be("all files ok");
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DryRunFileWithChangesPrintsFilePathAndReturnsOneWithoutModifyingFile()
    {
        var folder = GetTestCaseRequiringChanges();
        var originalPath = Path.Combine(_testCasesDir, folder, "original.cs.test");
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
        File.Copy(originalPath, tempPath);
        var originalContent = await File.ReadAllTextAsync(tempPath);
        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(["--dry-run", tempPath]);

            exitCode.Should().Be(1);
            output.ToString().Trim().Should().Be(tempPath);
            (await File.ReadAllTextAsync(tempPath)).Should().Be(originalContent);
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task DryRunFileWithNoChangesPrintsNoChangesAndReturnsZero()
    {
        var folder = (string)TestCases().First()[0];
        var cleanPath = Path.Combine(_testCasesDir, folder, "clean.cs.test");
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
        File.Copy(cleanPath, tempPath);
        var originalOut = Console.Out;
        var output = new StringWriter(new StringBuilder());
        Console.SetOut(output);

        try
        {
            var exitCode = await Program.Main(["--dry-run", tempPath]);

            exitCode.Should().Be(0);
            output.ToString().Trim().Should().Be("no changes");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task FileMode_CaseSensitiveMemberSortOption_IsApplied()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(
            tempPath,
            "class C\n{\n    private int B;\n    private int a;\n}\n"
        );

        try
        {
            var exitCode = await Program.Main(
                ["--sort-members-by-alphabet-case-sensitive", tempPath]
            );
            var content = await File.ReadAllTextAsync(tempPath);

            exitCode.Should().Be(0);
            content.IndexOf("private int B;", StringComparison.Ordinal).Should()
                .BeLessThan(content.IndexOf("private int a;", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task FileMode_DefaultMemberSort_IsCaseInsensitive()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(
            tempPath,
            "class C\n{\n    private int B;\n    private int a;\n}\n"
        );

        try
        {
            var exitCode = await Program.Main([tempPath]);
            var content = await File.ReadAllTextAsync(tempPath);

            exitCode.Should().Be(0);
            content.IndexOf("private int a;", StringComparison.Ordinal).Should()
                .BeLessThan(content.IndexOf("private int B;", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task FileMode_DefaultUsingSort_IsAlphabetical()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(tempPath, "using Zeta;\nusing System;\nusing Alpha;\n\nclass C {}\n");

        try
        {
            var exitCode = await Program.Main([tempPath]);
            var content = await File.ReadAllTextAsync(tempPath);

            exitCode.Should().Be(0);
            content.IndexOf("using Alpha;", StringComparison.Ordinal).Should()
                .BeLessThan(content.IndexOf("using System;", StringComparison.Ordinal));
            content.IndexOf("using System;", StringComparison.Ordinal).Should()
                .BeLessThan(content.IndexOf("using Zeta;", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task FileMode_SystemUsingFirstOption_PlacesSystemBeforeAlphabetical()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(tempPath, "using Zeta;\nusing System;\nusing Alpha;\n\nclass C {}\n");

        try
        {
            var exitCode = await Program.Main(["--system-using-first", tempPath]);
            var content = await File.ReadAllTextAsync(tempPath);

            exitCode.Should().Be(0);
            content.IndexOf("using System;", StringComparison.Ordinal).Should()
                .BeLessThan(content.IndexOf("using Alpha;", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static string GetTestCaseRequiringChanges()
    {
        var folder = Directory
            .EnumerateDirectories(_testCasesDir)
            .Select(dir => dir.Split(Path.DirectorySeparatorChar)[^1])
            .FirstOrDefault(testCase =>
            {
                var originalPath = Path.Combine(_testCasesDir, testCase, "original.cs.test");
                var cleanPath = Path.Combine(_testCasesDir, testCase, "clean.cs.test");
                return File.ReadAllText(originalPath) != File.ReadAllText(cleanPath);
            });

        return folder
            ?? throw new InvalidOperationException("No integration test case requiring changes found.");
    }
}