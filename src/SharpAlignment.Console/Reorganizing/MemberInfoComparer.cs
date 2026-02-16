using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpAlignment.Reorganizing;

public class MemberInfoComparer : IComparer<MemberInfo>
{
    private readonly Func<MemberInfo?, MemberInfo?, int>[] _compareMethods;
    private readonly MemberSortConfiguration _configuration;

    public MemberInfoComparer(MemberSortConfiguration configuration)
    {
        _configuration = configuration;
        _compareMethods =
        [
            CompareByMemberType,
            CompareByAccessModifier,
            CompareByAdditionalModifier,
            CompareByIdentifier,
        ];
    }

    public static MemberInfoComparer Default { get; } =
        new MemberInfoComparer(new MemberSortConfiguration());

    public int Compare(MemberInfo? x, MemberInfo? y)
    {
        return _compareMethods
            .Select(compareMethod => compareMethod(x, y))
            .FirstOrDefault(result => result != 0);
    }

    private int CompareByAccessModifier(MemberInfo? x, MemberInfo? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => x.AccessModifier - y.AccessModifier,
        };
    }

    private int CompareByAdditionalModifier(MemberInfo? x, MemberInfo? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => x.AdditionalModifier - y.AdditionalModifier,
        };
    }

    private int CompareByIdentifier(MemberInfo? x, MemberInfo? y)
    {
        if (!_configuration.SortByAlphabet)
        {
            return 0;
        }

        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => string.Compare(
                x.Identifier,
                y.Identifier,
                _configuration.SortByAlphabetCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase
            ),
        };
    }

    private int CompareByMemberType(MemberInfo? x, MemberInfo? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => x.MemberType - y.MemberType,
        };
    }
}