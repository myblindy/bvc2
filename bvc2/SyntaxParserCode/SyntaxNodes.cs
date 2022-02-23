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

record ClassDeclarationSyntaxNode(string Name, string[]? GenericTypes = null) : ParentSyntaxNode;

record BlockSyntaxNode : ParentSyntaxNode;

[Flags]
enum FunctionModifiers
{
    None = 0,
    Static = 1 << 0,
}

record FunctionDeclarationSyntaxNode(FunctionModifiers Modifiers, string Name, IdentifierExpressionSyntaxNode? ReturnType, (VariableModifiers Modifiers, string Name, IdentifierExpressionSyntaxNode Type)[] Arguments, bool Internal = false)
    : BlockSyntaxNode
{
    public const string PrimaryConstructorName = ".ctor";
    public bool IsPrimaryConstructor => Name == PrimaryConstructorName;

    public virtual bool Equals(FunctionDeclarationSyntaxNode? other) =>
        other is not null && base.Equals(other) && Modifiers == other.Modifiers && Name == other.Name && ReturnType == other.ReturnType && Internal == other.Internal && Arguments.SequenceEqual(other.Arguments);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        hash.Add(Modifiers);
        hash.Add(Name);
        hash.Add(ReturnType);
        foreach (var arg in Arguments)
        {
            hash.Add(arg.Modifiers);
            hash.Add(arg.Name);
            hash.Add(arg.Type);
        }

        return hash.ToHashCode();
    }
}

[Flags]
enum VariableModifiers
{
    None = 0,
    Val = 1 << 0,
    Static = 1 << 1,
}

record VariableSyntaxNode(VariableModifiers Modifiers, string Name, ExpressionSyntaxNode? ReturnType, ExpressionSyntaxNode? InitialValue, FunctionDeclarationSyntaxNode? Getter = null) : SyntaxNode;

abstract record ExpressionSyntaxNode : SyntaxNode;
record UnaryExpressionSyntaxNode(TokenType Operator, ExpressionSyntaxNode Right) : ExpressionSyntaxNode;
record BinaryExpressionSyntaxNode(ExpressionSyntaxNode Left, TokenType TokenType, ExpressionSyntaxNode Right) : ExpressionSyntaxNode;
record GroupingExpressionSyntaxNode(ExpressionSyntaxNode Expression) : ExpressionSyntaxNode;
record LiteralExpressionSyntaxNode(object Value) : ExpressionSyntaxNode;
record FunctionCallExpressionSyntaxNode(ExpressionSyntaxNode Expression, ExpressionSyntaxNode[] Arguments) : ExpressionSyntaxNode;
record StringExpressionSyntaxNode(ExpressionSyntaxNode[] Expressions) : ExpressionSyntaxNode;
record IdentifierExpressionSyntaxNode(string Identifier, ExpressionSyntaxNode[]? GenericParameters = null) : ExpressionSyntaxNode;

abstract record StatementSyntaxNode : SyntaxNode;
record ReturnStatementNode(ExpressionSyntaxNode Expression) : StatementSyntaxNode;