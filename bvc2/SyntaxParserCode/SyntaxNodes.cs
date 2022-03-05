using bvc2.LexerCode;

namespace bvc2.SyntaxParserCode;

abstract record SyntaxNode;

abstract record ParentSyntaxNode : SyntaxNode
{
    public List<SyntaxNode> Children { get; } = new();
}

record RootSyntaxNode : ParentSyntaxNode;

record EnumSyntaxNode(string Name) : ParentSyntaxNode;

record ClassSyntaxNode(string Name, string[]? GenericTypes = null) : ParentSyntaxNode;

record BlockSyntaxNode : ParentSyntaxNode;

[Flags]
enum FunctionModifiers
{
    None = 0,
    Static = 1 << 0,
}

record FunctionArgument(VariableModifiers Modifiers, string Name, IdentifierExpressionSyntaxNode Type);

record FunctionDeclarationSyntaxNode(FunctionModifiers Modifiers, string Name, IdentifierExpressionSyntaxNode? ReturnType, FunctionArgument[] Arguments, bool Internal = false)
    : BlockSyntaxNode
{
    public const string PrimaryConstructorName = ".ctor";
    public bool IsPrimaryConstructor => Name == PrimaryConstructorName;
}

record VariableSyntaxNode(VariableModifiers Modifiers, string Name, ExpressionSyntaxNode? ReturnType, ExpressionSyntaxNode? InitialValue, FunctionDeclarationSyntaxNode? Getter = null) : SyntaxNode;

abstract record ExpressionSyntaxNode : SyntaxNode;
record UnaryExpressionSyntaxNode(TokenType Operator, ExpressionSyntaxNode Right) : ExpressionSyntaxNode;
record BinaryExpressionSyntaxNode(ExpressionSyntaxNode Left, TokenType Operator, ExpressionSyntaxNode Right) : ExpressionSyntaxNode;
record GroupingExpressionSyntaxNode(ExpressionSyntaxNode Expression) : ExpressionSyntaxNode;
record LiteralExpressionSyntaxNode(object Value) : ExpressionSyntaxNode;
record FunctionCallExpressionSyntaxNode(ExpressionSyntaxNode Expression, ExpressionSyntaxNode[] Arguments) : ExpressionSyntaxNode;
record StringExpressionSyntaxNode(ExpressionSyntaxNode[] Expressions) : ExpressionSyntaxNode;
record IdentifierExpressionSyntaxNode(string Identifier, ExpressionSyntaxNode[]? GenericParameters = null) : ExpressionSyntaxNode;

abstract record StatementSyntaxNode : SyntaxNode;
record ReturnStatementNode(ExpressionSyntaxNode Expression) : StatementSyntaxNode;