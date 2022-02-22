using bvc2.LexerCode;

namespace bvc2.SyntaxParserCode;

abstract record SyntaxNode;

abstract record ParentSyntaxNode : SyntaxNode
{
    public List<SyntaxNode> Children { get; } = new();

    public virtual bool Equals(ParentSyntaxNode? other)
    {
        if (!base.Equals(other))
            return false;

        if (other.Children.Count != Children.Count)
            return false;

        for (int i = 0; i < Children.Count; ++i)
            if (!Children[i].Equals(other.Children[i]))
                return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        Children.ForEach(x => hash.Add(x.GetHashCode()));
        return hash.ToHashCode();
    }
}

record RootSyntaxNode : ParentSyntaxNode;

record EnumSyntaxNode(string Name) : ParentSyntaxNode
{
    public const VariableModifiers Modifiers = VariableModifiers.Val | VariableModifiers.Static;
}

[Flags]
enum VariableModifiers
{
    None = 0,
    Val = 1 << 0,
    Static = 1 << 1,
}

record VariableSyntaxNode(VariableModifiers Modifiers, string Name, ExpressionSyntaxNode? ReturnType, ExpressionSyntaxNode? InitialValue) : SyntaxNode;

abstract record ExpressionSyntaxNode : SyntaxNode;

record BinaryExpressionSyntaxNode(ExpressionSyntaxNode Left, TokenType TokenType, ExpressionSyntaxNode Right) : SyntaxNode;

record LiteralExpressionSyntaxNode(object Value) : ExpressionSyntaxNode;

record IdentifierExpressionSyntaxNode(string Identifier, ExpressionSyntaxNode[]? GenericParameters = null) : ExpressionSyntaxNode;