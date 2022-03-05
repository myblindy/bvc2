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

    public TypeSemanticEntry? FindType(ExpressionSyntaxNode expressionSyntaxNode) => null;

    public TypeSemanticEntry? FindType(string name) =>
        Children.OfType<TypeSemanticEntry>().FirstOrDefault(t => t.Name == name) ?? Parent?.FindType(name);
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

abstract record SemanticExpression;

record BinarySemanticExpression(SemanticExpression Left, TokenType Operator, SemanticExpression Right) : SemanticExpression;
record LiteralSemanticExpression(object Value) : SemanticExpression;