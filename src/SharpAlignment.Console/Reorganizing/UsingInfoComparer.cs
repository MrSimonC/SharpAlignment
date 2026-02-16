using System;
using System.Collections.Generic;

namespace SharpAlignment.Reorganizing;

public sealed class UsingInfoComparer : IComparer<UsingInfo>
{
    private readonly bool _systemUsingFirst;

    public UsingInfoComparer(bool systemUsingFirst)
    {
        _systemUsingFirst = systemUsingFirst;
    }

    public int Compare(UsingInfo? x, UsingInfo? y)
    {
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int result = x.CompareByIsGlobal(y);
        if (result != 0)
        {
            return result;
        }

        result = x.CompareByIsStatic(y);
        if (result != 0)
        {
            return result;
        }

        result = x.CompareByAlias(y);
        if (result != 0)
        {
            return result;
        }

        if (_systemUsingFirst)
        {
            var leftIsSystem = x.Name.StartsWith("System", StringComparison.Ordinal);
            var rightIsSystem = y.Name.StartsWith("System", StringComparison.Ordinal);
            if (leftIsSystem && !rightIsSystem)
            {
                return -1;
            }

            if (!leftIsSystem && rightIsSystem)
            {
                return 1;
            }
        }

        return x.CompareByName(y);
    }
}