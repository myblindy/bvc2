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
    public void Test1()
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
}
