using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpAlignment.Reorganizing;

namespace SharpAlignment.Syntax;

public static class UsingInfoFactory
{
    public static UsingInfo GetUsingInfo(this UsingDirectiveSyntax usingDirective)
    {
        return new UsingInfo(usingDirective.Name?.ToString() ?? string.Empty)
        {
            Alias = usingDirective.Alias?.Name.ToString(),
            IsStatic = usingDirective.StaticKeyword != default,
            IsGlobal = usingDirective.GlobalKeyword != default,
        };
    }
}