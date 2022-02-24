using bvc2.Common;
using bvc2.SyntaxParserCode;

namespace bvc2.Tests;

public class SyntaxParserTests
{
    static SyntaxParser GetSourceSyntaxParser(string source)
    {
        var inputStream = new MemoryStream();

        using (var writer = new StreamWriter(inputStream, Encoding.UTF8, leaveOpen: true))
            writer.Write(source);

        inputStream.Position = 0;
        return new(new LexerCode.Lexer(inputStream));
    }

    [Fact]
    public void BasicEnum()
    {
        var parser = GetSourceSyntaxParser(@"
enum E { V0, V1, V2 = 5, V3, V4 = 1, V5 }
");
        Assert.Equal(parser.Parse(), new RootSyntaxNode()
        {
            Children =
            {
                new EnumSyntaxNode("E")
                {
                    Children =
                    {
                        new VariableSyntaxNode(EnumSyntaxNode.Modifiers, "V0", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(0L)),
                        new VariableSyntaxNode(EnumSyntaxNode.Modifiers, "V1", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(1L)),
                        new VariableSyntaxNode(EnumSyntaxNode.Modifiers, "V2", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(5L)),
                        new VariableSyntaxNode(EnumSyntaxNode.Modifiers, "V3", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(6L)),
                        new VariableSyntaxNode(EnumSyntaxNode.Modifiers, "V4", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(1L)),
                        new VariableSyntaxNode(EnumSyntaxNode.Modifiers, "V5", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(2L)),
                    }
                }
            }
        });
    }

    [Fact]
    public void BasicVarInClass()
    {
        var parser = GetSourceSyntaxParser(@"
class C 
{
    var a = 10;
}
");
        Assert.Equal(parser.Parse(), new RootSyntaxNode()
        {
            Children =
            {
                new ClassDeclarationSyntaxNode("C")
                {
                    Children =
                    {
                        new VariableSyntaxNode(VariableModifiers.None, "a", null, new LiteralExpressionSyntaxNode(10L)),
                    }
                }
            }
        });
    }

    [Fact]
    public void PrimaryConstructor()
    {
        var parser = GetSourceSyntaxParser(@"
class C(var i: Integer, val d: Double);
");

        Assert.Equal(parser.Parse(), new RootSyntaxNode()
        {
            Children =
            {
                new ClassDeclarationSyntaxNode("C")
                {
                    Children =
                    {
                        new FunctionDeclarationSyntaxNode(FunctionModifiers.None, FunctionDeclarationSyntaxNode.PrimaryConstructorName, null, new[]
                        {
                            (VariableModifiers.None, "i", new IdentifierExpressionSyntaxNode("Integer")),
                            (VariableModifiers.Val, "d", new IdentifierExpressionSyntaxNode("Double")),
                        })
                    }
                }
            }
        });
    }
}
