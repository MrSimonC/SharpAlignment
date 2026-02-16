using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SharpAlignment.Syntax;

public static class SyntaxListExtensions
{
    public static SyntaxList<TNode> ToSyntaxList<TNode>(this IEnumerable<TNode> nodes)
        where TNode : SyntaxNode
    {
        return new SyntaxList<TNode>(nodes);
    }
}