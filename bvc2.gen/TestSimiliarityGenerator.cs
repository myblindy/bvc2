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
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) =>
                    node is TypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.Identifier.Text.EndsWith("SyntaxNode"),
                static (ctx, _) =>
                {
                    //if (!System.Diagnostics.Debugger.IsAttached)
                    //    System.Diagnostics.Debugger.Launch();

                    var tds = (TypeDeclarationSyntax)ctx.Node;
                    if (ctx.SemanticModel.GetDeclaredSymbol(tds) is { } symbol)
                        return new Class(tds.Identifier.Text,
                            symbol.GetMembers().OfType<IPropertySymbol>()
                                .Where(ps => !ps.IsStatic && ps.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
                                .Select(ps => new Field(ps.Name,
                                    isArray: ps.Type is IArrayTypeSymbol || ps.Type.OriginalDefinition.ToDisplayString() is "System.Collections.Generic.List<T>")));

                    return null!;
                })
            .Where(static m => m is not null);

        context.RegisterSourceOutput(classes.Collect(), static (spc, source) => Execute(source, spc));
    }

    static void AddSupportAsserts(StringBuilder sb)
    {
        foreach (var type in new[] { "string", "bool", "int", "bvc2.SyntaxParserCode.FunctionModifiers", "bvc2.LexerCode.TokenType", "bvc2.Common.VariableModifiers" })
            sb.AppendLine($"static void Assert({type} a, {type} b, string fieldName) {{")
                .Append("if(a != b)").AppendThrowLine()
                .AppendLine("}");
    }

    static void Execute(ImmutableArray<Class> classes, SourceProductionContext spc)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using bvc2.SyntaxParserCode;");
        sb.AppendLine("namespace bvc2.SyntaxParserCode.TestSupport;");

        sb.AppendLine("class AssertFailedException: Exception {")
            .AppendLine("public AssertFailedException(string msg) : base(msg) { }")
            .AppendLine("}");

        sb.AppendLine("static class SyntaxNodeSimilarity {");
        AddSupportAsserts(sb);
        sb.AppendLine("public static void Assert(SyntaxNode a, SyntaxNode b, string? fieldName = null) {");

        foreach (var c in classes)
        {
            sb.AppendLine($"if(a is {c.Name} a{c.Name} && b is {c.Name} b{c.Name}) {{");

            foreach (var f in c.Fields)
                if (f.IsArray)
                    sb.AppendLine($"Assert(a{c.Name}.{f.Name}.Count(), a{c.Name}.{f.Name}.Count(), \"{f.Name}.Count\");")
                        .AppendLine($"for(int i = 0; i < a{c.Name}.{f.Name}.Count(); ++i)")
                        .AppendLine($"Assert(a{c.Name}.{f.Name}[i], b{c.Name}.{f.Name}[i], \"{f.Name}[i]\");");
                else
                    sb.Append($"Assert(a{c.Name}.{f.Name}, b{c.Name}.{f.Name}, \"{f.Name}\");");

            sb.AppendLine("}");
        }

        sb.AppendLine("}}");

        //if (!System.Diagnostics.Debugger.IsAttached)
        //    System.Diagnostics.Debugger.Launch();
        spc.AddSource("SyntaxNodes.TestSupport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}

class Field
{
    public Field(string name, bool isArray) =>
        (Name, IsArray) = (name, isArray);

    public bool IsArray { get; }
    public string Name { get; }
}

class Class
{
    public string Name { get; }
    public ImmutableArray<Field> Fields { get; }

    public Class(string name, IEnumerable<Field> fields) =>
        (Name, Fields) = (name, ImmutableArray.CreateRange(fields));
}