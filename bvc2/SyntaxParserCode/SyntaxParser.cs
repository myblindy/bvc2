using bvc2.LexerCode;

namespace bvc2.SyntaxParserCode;

internal class SyntaxParser
{
    readonly Lexer lexer;

    public SyntaxParser(Lexer lexer) => this.lexer = lexer;

    Token? MatchTokenTypes(params TokenType[] tokenTypes)
    {
        var peekToken = lexer.Peek();
        if (peekToken is not null && tokenTypes.Contains(peekToken.Type))
        {
            lexer.Consume();
            return peekToken;
        }
        return null;
    }

    Token ExpectTokenTypes(params TokenType[] tokenTypes) =>
        MatchTokenTypes(tokenTypes) ?? throw new NotImplementedException();

    public RootSyntaxNode Parse()
    {
        var root = new RootSyntaxNode();
        ParseChildren(root, ParseContextType.Class);
        return root;
    }

    enum ParseContextType { Function, Class }
    void ParseChildren(ParentSyntaxNode node, ParseContextType parseContextType)
    {
        bool foundAny;

        do
        {
            foundAny = false;

            // enum
            if (parseContextType is ParseContextType.Class)
                while (MatchTokenTypes(TokenType.EnumKeyword) is { })
                {
                    var enumName = ExpectTokenTypes(TokenType.Identifier).Text;
                    var enumNode = new EnumSyntaxNode(enumName);
                    ExpectTokenTypes(TokenType.OpenBrace);

                    var nextValue = 0L;
                    while (MatchTokenTypes(TokenType.Identifier) is { } identifier)
                    {
                        if (MatchTokenTypes(TokenType.Equals) is { })
                            nextValue = ((IntegerLiteralToken)ExpectTokenTypes(TokenType.IntegerLiteral)).Value + 1;
                        else
                            ++nextValue;

                        enumNode.Children.Add(new VariableSyntaxNode(EnumSyntaxNode.Modifiers, identifier.Text, 
                            new IdentifierExpressionSyntaxNode(enumName), new LiteralExpressionSyntaxNode(nextValue - 1)));

                        if (MatchTokenTypes(TokenType.Comma) is null)
                            break;
                    }

                    node.Children.Add(enumNode);
                }
        } while (foundAny);
    }
}
