using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;

namespace SharpAlignment;

public class RootCommandConfigurationBinder : BinderBase<RootCommandConfiguration>
{
    private readonly Option<bool> _dryRun;
    private readonly Option<string[]> _exclude;
    private readonly Argument<FileSystemInfo?> _input;
    private readonly Option<bool> _noSortMembersByAlphabet;
    private readonly Option<bool> _sortMembersByAlphabetCaseSensitive;
    private readonly Option<bool> _systemUsingFirst;

    public RootCommandConfigurationBinder(
        Option<bool> noSortMembersByAlphabet,
        Option<bool> sortMembersByAlphabetCaseSensitive,
        Option<bool> dryRun,
        Option<string[]> exclude,
        Option<bool> systemUsingFirst,
        Argument<FileSystemInfo?> input
    )
    {
        _noSortMembersByAlphabet = noSortMembersByAlphabet;
        _sortMembersByAlphabetCaseSensitive = sortMembersByAlphabetCaseSensitive;
        _dryRun = dryRun;
        _exclude = exclude;
        _systemUsingFirst = systemUsingFirst;
        _input = input;
    }

    protected override RootCommandConfiguration GetBoundValue(BindingContext bindingContext)
    {
        var parseResult = bindingContext.ParseResult;

        var input = parseResult.GetValueForArgument(_input);
        var mode =
            input is null
                ? InputOutputMode.Console
                : input is DirectoryInfo
                    ? InputOutputMode.Directory
                    : InputOutputMode.File;

        return new RootCommandConfiguration
        {
            SortMembersByAlphabet = !bindingContext.ParseResult.GetValueForOption(
                _noSortMembersByAlphabet
            ),
            SortMembersByAlphabetCaseSensitive = bindingContext.ParseResult.GetValueForOption(
                _sortMembersByAlphabetCaseSensitive
            ),
            DryRun = bindingContext.ParseResult.GetValueForOption(_dryRun),
            ExcludedPaths = bindingContext.ParseResult.GetValueForOption(_exclude) ?? [],
            SystemUsingFirst = bindingContext.ParseResult.GetValueForOption(_systemUsingFirst),
            Mode = mode,
            File = input as FileInfo,
            Directory = input as DirectoryInfo,
            Console = bindingContext.Console,
        };
    }
}