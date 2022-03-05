using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace bvc2.gen;

[Generator]
class TestSimiliarityGenerator : IIncrementalGenerator
{
    internal const string SyntaxNodeSuffix = "SyntaxNode";
    internal const string SemanticEntrySuffix = "SemanticEntry";
    internal const string SemanticExpressionSuffix = "SemanticExpression";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        static bool isInteresting(TypeDeclarationSyntax tds) =>
            tds.Identifier.Text.EndsWith(SyntaxNodeSuffix) || tds.Identifier.Text.EndsWith(SemanticEntrySuffix) || tds.Identifier.Text.EndsWith(SemanticExpressionSuffix);

        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax typeDeclarationSyntax && isInteresting(typeDeclarationSyntax),
                static (ctx, _) =>
                {
                    //!System.Diagnostics.Debugger.IsAttached)
                    //    System.Diagnostics.Debugger.Launch();

                    var tds = (TypeDeclarationSyntax)ctx.Node;
                    if (ctx.SemanticModel.GetDeclaredSymbol(tds) is { } symbol)
                        return new Class(tds.Identifier.Text,
                            symbol.GetMembers().OfType<IPropertySymbol>()
                                .Where(ps => !ps.IsStatic && ps.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
                                .Select(ps => new Field(ps.Name,
                                    isArray: ps.Type is IArrayTypeSymbol || ps.Type.OriginalDefinition.ToDisplayString() is "System.Collections.Generic.List<T>" or "System.Collections.ObjectModel.ReadOnlyCollection<T>"
                                        || ps.Type.HasBaseType("System.Collections.ObjectModel.Collection<T>"))));

                    return null!;
                })
            .Where(static m => m is not null);

        context.RegisterSourceOutput(classes.Collect(), static (spc, source) => Execute(source, spc));
    }

    static void AddSupportAsserts(StringBuilder sb)
    {
        foreach (var type in new[] { "string?", "bool", "long", "double", "bvc2.SyntaxParserCode.FunctionModifiers", "bvc2.LexerCode.TokenType", "bvc2.Common.VariableModifiers" })
            sb.AppendLine($"static void Assert({type} a, {type} b, string fieldName, HashSet<object> done) {{")
                .Append("if(a != b)").AppendThrowLine()
                .AppendLine("}");

        sb.AppendLine("static void Assert(FunctionArgument a, FunctionArgument b, string fieldName, HashSet<object> done) {")
            .AppendLine("Assert(a.Modifiers, b.Modifiers, fieldName, done);")
            .AppendLine("Assert(a.Name, b.Name, fieldName, done);")
            .AppendLine("Assert(a.Type, b.Type, fieldName, done);")
            .AppendLine("}");

        sb.AppendLine("static void Assert(object? a, object? b, string fieldName, HashSet<object> done) {")
            .AppendLine("if(a is long aLong && b is long bLong) Assert(aLong, bLong, fieldName, done);")
            .AppendLine("else if(a is double aDouble && b is double bDouble) Assert(aDouble, bDouble, fieldName, done);")
            .AppendLine("else if(a is string aString && b is string bString) Assert(aString, bString, fieldName, done);")
            .AppendLine("else throw new NotImplementedException($\"Invalid type assertion: a is {a.GetType().Name} and b is {b.GetType().Name}.\");")
            .AppendLine("}");
    }

    static void Execute(ImmutableArray<Class> classes, SourceProductionContext spc)
    {
        var sb = new StringBuilder();

        void writeAssertBody(ClassType classType)
        {
            foreach (var c in classes.Where(c => c.Type == classType))
            {
                sb!.AppendLine($"if(a is {c.Name} a{c.Name} && b is {c.Name} b{c.Name}) {{");

                foreach (var f in c.Fields.Where(f => f.Name is not "Parent"))
                    if (f.IsArray)
                        sb.AppendLine($"if(a{c.Name}.{f.Name} is not null && b{c.Name}.{f.Name} is not null) {{")
                            .AppendLine($"var fa = a{c.Name}.{f.Name}.Where(w => !w.IsInternal()).ToList();")
                            .AppendLine($"var fb = b{c.Name}.{f.Name}.Where(w => !w.IsInternal()).ToList();")
                            .AppendLine($"Assert(fa.Count(), fb.Count(), \"{f.Name}.Count\", done);")
                            .AppendLine($"for(int i = 0; i < fa.Count(); ++i)")
                            .AppendLine($"Assert(fa[i], fb[i], $\"{f.Name}[{{i}}]\", done);")
                            .AppendLine("}");
                    else
                        sb.Append($"Assert(a{c.Name}.{f.Name}, b{c.Name}.{f.Name}, \"{f.Name}\", done);");

                sb.AppendLine("}");
            }
        }

        sb.AppendLine("#nullable enable")
            .AppendLine("using bvc2.SyntaxParserCode;")
            .AppendLine("using bvc2.SemanticParserCode;")
            .AppendLine("namespace bvc2.BvcEntities.TestSupport;");

        sb.AppendLine("class AssertFailedException: Exception {")
            .AppendLine("public AssertFailedException(string msg) : base(msg) { }")
            .AppendLine("}");

        sb.AppendLine("static class TestSupportExtensions {")
            .AppendLine("public static bool IsInternal(this object? o) {")
            .AppendLine("if(o is null) return false;")
            .AppendLine("return (bool)(o.GetType().GetProperty(\"Internal\")?.GetValue(o) ?? false);")
            .AppendLine("}}");

        sb.AppendLine("static class SyntaxNodeSimilarity {");
        AddSupportAsserts(sb);
        sb.AppendLine("public static void Assert(SyntaxNode? a, SyntaxNode? b, string? fieldName = null, HashSet<object>? done = null) {")
            .AppendLine("done ??= new();")
            .Append("if((a is null && b is not null) || (a is not null && b is null))").AppendThrowLine()
            .AppendLine("if(a is null && b is null) return;")
            .AppendLine("if(done.Contains(a!) && done.Contains(b!)) return;")
            .AppendLine("done.Add(a!); done.Add(b!);");
        writeAssertBody(ClassType.SyntaxNode);
        sb.AppendLine("}}");

        sb.AppendLine("static class SemanticEntrySimilarity {");
        AddSupportAsserts(sb);

        sb.AppendLine("public static void Assert(SemanticEntry? a, SemanticEntry? b, string? fieldName = null, HashSet<object>? done = null) {")
            .AppendLine("done ??= new();")
            .Append("if((a is null && b is not null) || (a is not null && b is null))").AppendThrowLine()
            .AppendLine("if(a is null && b is null) return;")
            .AppendLine("if(done.Contains(a!) && done.Contains(b!)) return;")
            .AppendLine("done.Add(a!); done.Add(b!);");
        writeAssertBody(ClassType.SemanticEntry);
        sb.AppendLine("}");

        sb.AppendLine("public static void Assert(SemanticExpression? a, SemanticExpression? b, string? fieldName = null, HashSet<object>? done = null) {")
            .AppendLine("done ??= new();")
            .Append("if((a is null && b is not null) || (a is not null && b is null))").AppendThrowLine()
            .AppendLine("if(a is null && b is null) return;")
            .AppendLine("if(done.Contains(a!) && done.Contains(b!)) return;")
            .AppendLine("done.Add(a!); done.Add(b!);");
        writeAssertBody(ClassType.SemanticExpression);
        sb.AppendLine("}");

        sb.AppendLine("}");

        spc.AddSource("BvcEntities.TestSupport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}

class Field
{
    public Field(string name, bool isArray) =>
        (Name, IsArray) = (name, isArray);

    public bool IsArray { get; }
    public string Name { get; }
}

enum ClassType { SyntaxNode, SemanticEntry, SemanticExpression }

class Class
{
    public string Name { get; }
    public ClassType Type { get; }
    public ImmutableArray<Field> Fields { get; }

    public Class(string name, IEnumerable<Field> fields) =>
        (Name, Fields, Type) = (name, ImmutableArray.CreateRange(fields),
            name.EndsWith(TestSimiliarityGenerator.SemanticEntrySuffix) ? ClassType.SemanticEntry
                : name.EndsWith(TestSimiliarityGenerator.SyntaxNodeSuffix) ? ClassType.SyntaxNode
                : name.EndsWith(TestSimiliarityGenerator.SemanticExpressionSuffix) ? ClassType.SemanticExpression
                : throw new InvalidOperationException($"Invalid class type detected for {name}."));
}