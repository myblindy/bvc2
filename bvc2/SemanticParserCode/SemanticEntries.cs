using bvc2.LexerCode;
using bvc2.SyntaxParserCode;

namespace bvc2.SemanticParserCode;

abstract class SemanticEntry : IEquatable<SemanticEntry>
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

    #region equality
    public bool Equals(SemanticEntry? other) =>
        other is not null && Name == other.Name && Internal == other.Internal && Children.Where(c => !c.Internal).SequenceEqual(other.Children.Where(c => !c.Internal));

    public override bool Equals(object? obj) => Equals(obj as SemanticEntry);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        hash.Add(Internal);
        foreach (var child in Children.Where(c => !c.Internal))
            hash.Add(child);
        return hash.ToHashCode();
    }

    public static bool operator ==(SemanticEntry? a, SemanticEntry? b) => a is not null && b is not null && a.Equals(b) || a is null && b is null;
    public static bool operator !=(SemanticEntry? a, SemanticEntry? b) => !(a == b);
    #endregion
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

class VariableSemanticEntry : SemanticEntry, IEquatable<VariableSemanticEntry>
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

    #region equality
    public bool Equals(VariableSemanticEntry? other) =>
        Equals((SemanticEntry?)other) && Modifiers == other.Modifiers && Type == other.Type && InitialValue == other.InitialValue;

    public override bool Equals(object? obj) => Equals(obj as VariableSemanticEntry);

    public override int GetHashCode() =>
        HashCode.Combine(base.GetHashCode(), Modifiers, Type, InitialValue);

    public static bool operator ==(VariableSemanticEntry? a, VariableSemanticEntry? b) => a is not null && b is not null && a.Equals(b) || a is null && b is null;
    public static bool operator !=(VariableSemanticEntry? a, VariableSemanticEntry? b) => !(a == b);
    #endregion
}

abstract class TypeSemanticEntry : SemanticEntry, IEquatable<TypeSemanticEntry>
{
    public ReadOnlyCollection<string> GenericParameters { get; }

    public TypeSemanticEntry(string name, string[] genericParameters) : base(name) =>
        GenericParameters = new(genericParameters);

    #region equality
    public bool Equals(TypeSemanticEntry? other) =>
        Equals((SemanticEntry?)other) && GenericParameters.SequenceEqual(other.GenericParameters);

    public override bool Equals(object? obj) => Equals(obj as TypeSemanticEntry);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var genericParameter in GenericParameters)
            hash.Add(genericParameter);
        return hash.ToHashCode();
    }

    public static bool operator ==(TypeSemanticEntry? a, TypeSemanticEntry? b) => a is not null && b is not null && a.Equals(b) || a is null && b is null;
    public static bool operator !=(TypeSemanticEntry? a, TypeSemanticEntry? b) => !(a == b);
    #endregion
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

    #region equality
    public bool Equals(ClassSemanticEntry? other) => Equals((TypeSemanticEntry?)other);

    public override bool Equals(object? obj) => Equals(obj as ClassSemanticEntry);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(ClassSemanticEntry? a, ClassSemanticEntry? b) => a is not null && b is not null && a.Equals(b) || a is null && b is null;
    public static bool operator !=(ClassSemanticEntry? a, ClassSemanticEntry? b) => !(a == b);
    #endregion
}

abstract record SemanticExpression;

record BinarySemanticExpression(SemanticExpression Left, TokenType Operator, SemanticExpression Right) : SemanticExpression;
record LiteralSemanticExpression(object Value) : SemanticExpression;