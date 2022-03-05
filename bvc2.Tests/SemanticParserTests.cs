using bvc2.Common;
using bvc2.SemanticParserCode;
using bvc2.SyntaxParserCode;
using bvc2.BvcEntities.TestSupport;

namespace bvc2.Tests;

public class SemanticParserTests
{
    static SemanticParser GetSourceSemanticParser(string source)
    {
        var inputStream = new MemoryStream();

        using (var writer = new StreamWriter(inputStream, Encoding.UTF8, leaveOpen: true))
            writer.Write(source);

        inputStream.Position = 0;

        var syntaxParser = new SyntaxParser(new LexerCode.Lexer(inputStream));
        return new(syntaxParser.Parse());
    }

    [Fact]
    public void BasicEnum()
    {
        var parser = GetSourceSemanticParser(@"
enum E { V0, V1 = 5, V2, V3 = 0, V4 }
");

        var typeE = new EnumSemanticEntry("E");
        typeE.Children.Add(new VariableSemanticEntry(VariableModifiers.Enum, "V0", typeE, new LiteralSemanticExpression(0L)));
        typeE.Children.Add(new VariableSemanticEntry(VariableModifiers.Enum, "V1", typeE, new LiteralSemanticExpression(5L)));
        typeE.Children.Add(new VariableSemanticEntry(VariableModifiers.Enum, "V2", typeE, new LiteralSemanticExpression(6L)));
        typeE.Children.Add(new VariableSemanticEntry(VariableModifiers.Enum, "V3", typeE, new LiteralSemanticExpression(0L)));
        typeE.Children.Add(new VariableSemanticEntry(VariableModifiers.Enum, "V4", typeE, new LiteralSemanticExpression(1L)));

        SemanticEntrySimilarity.Assert(parser.Parse(), new RootSemanticEntry()
        {
            Children = { typeE }
        });
    }

    [Fact]
    public void BasicClass()
    {
        var parser = GetSourceSemanticParser(@"
class C
{
    var a = 10 + 5;
}
");
        var result = parser.Parse();

        var typeC = new ClassSemanticEntry("C", Array.Empty<string>());
        typeC.Children.Add(new VariableSemanticEntry(VariableModifiers.None, "a", result.FindType("Integer"),
            new BinarySemanticExpression(new LiteralSemanticExpression(10L), LexerCode.TokenType.Plus, new LiteralSemanticExpression(5L))));

        SemanticEntrySimilarity.Assert(result, new RootSemanticEntry()
        {
            Children = { typeC }
        });
    }
}
