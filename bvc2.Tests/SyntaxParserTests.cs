using bvc2.Common;
using bvc2.SyntaxParserCode;
using bvc2.BvcEntities.TestSupport;

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
        SyntaxNodeSimilarity.Assert(parser.Parse(), new RootSyntaxNode()
        {
            Children =
            {
                new EnumSyntaxNode("E")
                {
                    Children =
                    {
                        new VariableSyntaxNode(VariableModifiers.Enum, "V0", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(0L)),
                        new VariableSyntaxNode(VariableModifiers.Enum, "V1", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(1L)),
                        new VariableSyntaxNode(VariableModifiers.Enum, "V2", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(5L)),
                        new VariableSyntaxNode(VariableModifiers.Enum, "V3", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(6L)),
                        new VariableSyntaxNode(VariableModifiers.Enum, "V4", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(1L)),
                        new VariableSyntaxNode(VariableModifiers.Enum, "V5", new IdentifierExpressionSyntaxNode("E"), new LiteralExpressionSyntaxNode(2L)),
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
        SyntaxNodeSimilarity.Assert(parser.Parse(), new RootSyntaxNode()
        {
            Children =
            {
                new ClassSyntaxNode("C")
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

        SyntaxNodeSimilarity.Assert(parser.Parse(), new RootSyntaxNode()
        {
            Children =
            {
                new ClassSyntaxNode("C")
                {
                    Children =
                    {
                        new FunctionDeclarationSyntaxNode(FunctionModifiers.None, FunctionDeclarationSyntaxNode.PrimaryConstructorName, null, new FunctionSyntaxParameter[]
                        {
                            new(VariableModifiers.None, "i", new IdentifierExpressionSyntaxNode("Integer")),
                            new(VariableModifiers.Val, "d", new IdentifierExpressionSyntaxNode("Double")),
                        })
                    }
                }
            }
        });
    }


    [Fact]
    public void ShouldFail()
    {
        var parser = GetSourceSyntaxParser(@"
class C(var i: Integer, val d: Double);
");

        Assert.Throws<AssertFailedException>(() => SyntaxNodeSimilarity.Assert(parser.Parse(), new RootSyntaxNode()
        {
            Children =
            {
                new ClassSyntaxNode("C")
                {
                    Children =
                    {
                        new FunctionDeclarationSyntaxNode(FunctionModifiers.None, FunctionDeclarationSyntaxNode.PrimaryConstructorName, null, new FunctionSyntaxParameter[]
                        {
                            new(VariableModifiers.None, "i", new IdentifierExpressionSyntaxNode("Meep")),
                            new(VariableModifiers.Val, "d", new IdentifierExpressionSyntaxNode("Moop")),
                        })
                    }
                }
            }
        }));
    }
}
