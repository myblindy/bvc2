using bvc2.SemanticParserCode;
using bvc2.SyntaxParserCode;

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
        typeE.Children.Add(new VariableSemanticEntry(EnumSyntaxNode.Modifiers, "V0", typeE, new LiteralSemanticExpression(0L)));
        typeE.Children.Add(new VariableSemanticEntry(EnumSyntaxNode.Modifiers, "V1", typeE, new LiteralSemanticExpression(5L)));
        typeE.Children.Add(new VariableSemanticEntry(EnumSyntaxNode.Modifiers, "V2", typeE, new LiteralSemanticExpression(6L)));
        typeE.Children.Add(new VariableSemanticEntry(EnumSyntaxNode.Modifiers, "V3", typeE, new LiteralSemanticExpression(0L)));
        typeE.Children.Add(new VariableSemanticEntry(EnumSyntaxNode.Modifiers, "V4", typeE, new LiteralSemanticExpression(1L)));

        Assert.Equal(parser.Parse(), new RootSemanticEntry()
        {
            Children =
            {
                typeE
            }
        });
    }
}
