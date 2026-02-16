# sharpalignment dotnet tool

A [dotnet tool](https://www.nuget.org/packages/sharpalignment) for your `C#` files at your service.

This tool is heavily inspired by [CodeMaid](https://www.codemaid.net).

## Features

Using this tool will cleanup your `C#` file by

1.  reorganizing the layout of the members in the C# file to follow Microsoft's StyleCop conventions
2.  sorting it's using directives
3.  removes trailing whitespace and consecutive blank lines

### Options

- `--no-sort-members-by-alphabet`: Disables sorting members by alphabet. [default: `false`]
- `--sort-members-by-alphabet-case-sensitive`: Enables case-sensitive sorting for member identifiers. [default: `false`]
- `--dry-run`: Does not modify files. For a file, prints `no changes` and exits with `0` when no update is needed, or prints the file path and exits with `1` when changes would be made. For a directory, scans `*.cs` files recursively, prints affected file paths, and exits with `0` (`all files ok`) or `1` (changes found). [default: `false`]
- `--exclude`: Excludes one or more file or directory paths from directory scanning. Can be supplied multiple times.
- `--system-using-first`: Places using directives starting with `System` before other usings. [default: `false`]
- `--version`: Prints the tool version and exits. [default: `false`]

### `--help` output

Current CLI help output:

```text
Description:
  Reorganises, sorts and cleans up the provided C# file.

Usage:
  sharpalignment [<input>] [options]

Arguments:
  <input>  Path to input file or directory, or piped input. []

Options:
  --no-sort-members-by-alphabet              Disables sorting members by alphabet. [default: False]
  --sort-members-by-alphabet-case-sensitive  Enables case-sensitive sorting for member identifiers. [default: False]
  --dry-run                                  Reports what would change without modifying files. [default: False]
  --exclude <exclude>                        Excludes one or more file or directory paths from directory scanning.
  --system-using-first                       Places using directives starting with 'System' before other usings. [default: False]
  --version                                  Show version information
  -?, -h, --help                             Show help and usage information
```

### Reorganize the layout of members in a C# file to follow Microsoft's StyleCop conventions

First by type:

1. Field
2. Constructor
3. Destructor
4. Delegate
5. Event
6. Enum
7. Interface
8. Property
9. Indexer
10. Operator
11. Method
12. Struct
13. Class

Then by access modifier:

1.  `public`
2.  `internal`
3.  `protected`
4.  `protected internal`
5.  `private protected`
6.  `private`

Then by additional modifiers:

1.  `const`
2.  `static readonly`
3.  `static`
4.  `readonly`
5.  none

And finally alphabetically (optional, case-insensitive by default).

**Warning:** `#region ... #endregion` is not supported.

### Sort using directives

Sorts using directives alphabetically and takes into account the following order:

1. "Normal" using directives
2. Aliased using statements (e.g. `using MyAlias = Example.Bar`)
3. Static using statements (e.g. `using static System.Math`)

Example:

```csharp
using System;
using Example;
using Example.Foo;
using MyAlias = Example.Bar;
using static System.Math;
```

Use `--system-using-first` to place `System` directives before other usings.

### Removes trailing whitespace and consecutive blank lines

- Removes trailing whitespace.
- Removes consecutive blank lines.

## Usage

Self-contained single-file binaries are produced for Linux (`linux-x64`) and Windows (`win-x64`) and include the runtime.

Install SharpAlignment as a global tool with the following command.

```sh
dotnet tool install --global sharpalignment
```

If you use the global tool install above, you still need the [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

## Release Automation

The repository includes a GitHub Actions workflow at `.github/workflows/release-binaries.yml` that:

1. Runs on push to `master`/`main` when `src/Directory.Build.props` changes (or manual dispatch).
2. Reads the `<Version>` from `src/Directory.Build.props`.
3. Validates the internal binary version (`--version`) matches that same version.
4. Publishes self-contained single-file binaries for Linux (`linux-x64`) and Windows (`win-x64`).
5. Creates a GitHub release with tag `sharpalignment-v<Version>` and uploads both binaries.

Then use the tool to cleanup your code.

```sh
# File mode
sharpalignment path/to/MyClass.cs

# Directory mode
sharpalignment path/to/csharp-project
# Scans *.cs files recursively in the directory

# Pipe mode
type path/to/MyClass.cs | sharpalignment > MyClass.Reorganized.cs`

# Do not sort members by alphabet
sharpalignment path/to/MyClass.cs --no-sort-members-by-alphabet

# Sort members by alphabet case-sensitively
sharpalignment path/to/MyClass.cs --sort-members-by-alphabet-case-sensitive

# Dry run
sharpalignment path/to/MyClass.cs --dry-run
# Dry run on a file: exit code 0 with "no changes", or 1 with the file path
# Dry run on a directory: exit code 0 with "all files ok", or 1 with affected file paths
sharpalignment --dry-run path/to/csharp-project
# Exclude one or more paths while scanning a directory
sharpalignment --dry-run --exclude path/to/csharp-project/bin --exclude path/to/csharp-project/obj path/to/csharp-project

# Place System usings first
sharpalignment path/to/MyClass.cs --system-using-first

# Show version
sharpalignment --version
```

## Acknowledgements

- CodeMaid - but doesn't support CLI: https://github.com/codecadwallader/codemaid
- code butler - https://github.com/just-seba/code-butler - which formed the basis of which this app was grown.