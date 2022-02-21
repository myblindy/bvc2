using bvc2.LexerCode;

namespace bvc2.Tests;

public class LexerTests
{
    static Lexer GetSourceLexer(string source)
    {
        var inputStream = new MemoryStream();

        using (var writer = new StreamWriter(inputStream, Encoding.UTF8, leaveOpen: true))
            writer.Write(source);

        inputStream.Position = 0;
        return new(inputStream);
    }

    static IEnumerable<Token> EnumerateTokens(Lexer lexer)
    {
        while (lexer.Consume() is { } token)
            yield return token;
    }

    [Fact]
    public void BasicVariableStatement()
    {
        var lexer = GetSourceLexer(@"
var a = 10;
");

        Assert.Equal(EnumerateTokens(lexer), new Token[]
        {
            new KeywordToken("var", TokenType.VarKeyword),
            new IdentifierToken("a"),
            new SymbolToken("=", TokenType.Equals),
            new IntegerLiteralToken(10),
            new SymbolToken(";", TokenType.SemiColon)
        });
    }

    [Fact]
    public void BasicLiteralExpression()
    {
        var lexer = GetSourceLexer(@"
var a = 100 + (5 / 3);
var b = a * 1.2;
");

        Assert.Equal(EnumerateTokens(lexer), new Token[]
        {
            new KeywordToken("var", TokenType.VarKeyword),
            new IdentifierToken("a"),
            new SymbolToken("=", TokenType.Equals),
            new IntegerLiteralToken(100),
            new SymbolToken("+", TokenType.Plus),
            new SymbolToken("(", TokenType.OpenParentheses),
            new IntegerLiteralToken(5),
            new SymbolToken("/", TokenType.Slash),
            new IntegerLiteralToken(3),
            new SymbolToken(")", TokenType.CloseParentheses),
            new SymbolToken(";", TokenType.SemiColon),

            new KeywordToken("var", TokenType.VarKeyword),
            new IdentifierToken("b"),
            new SymbolToken("=", TokenType.Equals),
            new IdentifierToken("a"),
            new SymbolToken("*", TokenType.Star),
            new DoubleLiteralToken(1.2),
            new SymbolToken(";", TokenType.SemiColon)
        });
    }
}
