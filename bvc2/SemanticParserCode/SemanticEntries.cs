using bvc2.LexerCode;
using bvc2.SyntaxParserCode;

namespace bvc2.SemanticParserCode;

abstract class SemanticEntry
{
    public string? Name { get; }
    public bool Internal { get; init; }
    public SemanticEntry? Parent { get; set; }
    public SemanticEntryCollection Children { get; }

    public SemanticEntry(string? name) =>
        (Name, Children) = (name, new(this));

    public SemanticEntry Root
    {
        get
        {
            var p = this;
            while (p.Parent is not null)
                p = p.Parent;
            return p;
        }
    }

    public FunctionSemanticEntry? FindFunction(ExpressionSyntaxNode? identifier, TypeSemanticEntry[] parameterTypes)
    {
        if (identifier is not LiteralExpressionSyntaxNode { Value: string } literalExpressionSyntaxNode)
            return null;

        return Children.OfType<FunctionSemanticEntry>().FirstOrDefault(f =>
            f.Name == (string)literalExpressionSyntaxNode.Value && f.Parameters.Select(w => w.Type).SequenceEqual(parameterTypes));
    }

    public TypeSemanticEntry? FindType(ExpressionSyntaxNode? identifier)
    {
        switch (identifier)
        {
            case null: return null;
            case LiteralExpressionSyntaxNode { Value: string } stringLiteralExpressionSyntaxNode:
                return Children.OfType<TypeSemanticEntry>().FirstOrDefault(t => t.Name == (string)stringLiteralExpressionSyntaxNode.Value) ?? Parent?.FindType(identifier);
            case LiteralExpressionSyntaxNode literalExpressionSyntaxNode:
                return literalExpressionSyntaxNode.Value switch
                {
                    long longValue => Root.FindType(BasicTypeNames.Integer),
                    double doubleValue => Root.FindType(BasicTypeNames.Double),
                    _ => throw new NotImplementedException()
                };
            case BinaryExpressionSyntaxNode binaryExpressionSyntaxNode:
                var leftType = FindType(binaryExpressionSyntaxNode.Left);
                if (leftType is null) return null;
                var rightType = FindType(binaryExpressionSyntaxNode.Right);
                if (rightType is null) return null;

                var name = binaryExpressionSyntaxNode.Operator switch
                {
                    TokenType.Plus => "+",
                    _ => throw new NotImplementedException()
                };
                return leftType.FindFunction(new LiteralExpressionSyntaxNode(name), new[] { leftType, rightType })?.ReturnType
                    ?? rightType.FindFunction(new LiteralExpressionSyntaxNode(name), new[] { leftType, rightType })?.ReturnType;
            default: throw new NotImplementedException();
        }
    }

    public TypeSemanticEntry? FindType(string name) => FindType(new LiteralExpressionSyntaxNode(name));
}

class SemanticEntryCollection : Collection<SemanticEntry>
{
    readonly SemanticEntry entry;

    public SemanticEntryCollection(SemanticEntry entry) =>
        this.entry = entry;

    protected override void SetItem(int index, SemanticEntry item)
    {
        base.SetItem(index, item);
        item.Parent = entry;
    }

    protected override void InsertItem(int index, SemanticEntry item)
    {
        base.InsertItem(index, item);
        item.Parent = entry;
    }
}

class RootSemanticEntry : SemanticEntry
{
    public RootSemanticEntry() : base(null)
    {
    }
}

class VariableSemanticEntry : SemanticEntry
{
    public VariableSemanticEntry(VariableModifiers modifiers, string name, TypeSemanticEntry? type, SemanticExpression? initialValue)
        : base(name)
    {
        Modifiers = modifiers;
        Type = type;
        InitialValue = initialValue;
    }

    public VariableModifiers Modifiers { get; }
    public TypeSemanticEntry? Type { get; }
    public SemanticExpression? InitialValue { get; }
}

abstract class TypeSemanticEntry : SemanticEntry
{
    public ReadOnlyCollection<string> GenericParameters { get; }

    public TypeSemanticEntry(string name, string[] genericParameters) : base(name) =>
        GenericParameters = new(genericParameters);
}

class EnumSemanticEntry : TypeSemanticEntry
{
    public EnumSemanticEntry(string name) : base(name, Array.Empty<string>())
    {
    }
}

class ClassSemanticEntry : TypeSemanticEntry
{
    public ClassSemanticEntry(string name, string[] genericParameters) : base(name, genericParameters)
    {
    }
}

record FunctionSemanticParameter(VariableModifiers Modifiers, string Name, TypeSemanticEntry? Type);
class FunctionSemanticEntry : TypeSemanticEntry
{
    public FunctionModifiers Modifiers { get; }
    public TypeSemanticEntry? ReturnType { get; }
    public ReadOnlyCollection<FunctionSemanticParameter> Parameters { get; }

    public FunctionSemanticEntry(FunctionModifiers modifiers, string name, string[] genericParameters, TypeSemanticEntry? returnType,
        FunctionSemanticParameter[] parameters) : base(name, genericParameters) =>
        (Modifiers, ReturnType, Parameters) = (modifiers, returnType, new(parameters));
}

abstract record SemanticExpression(TypeSemanticEntry? Type);

record BinarySemanticExpression(SemanticExpression Left, TokenType Operator, SemanticExpression Right, TypeSemanticEntry? Type) : SemanticExpression(Type);
record LiteralSemanticExpression(object Value, TypeSemanticEntry? Type) : SemanticExpression(Type);