using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SharpAlignment.Syntax;

public static class SyntaxListExtensions
{
    public static SyntaxList<TNode> ToSyntaxList<TNode>(this IEnumerable<TNode> nodes)
        where TNode : SyntaxNode
    {
        return new SyntaxList<TNode>(nodes);
    }
}
