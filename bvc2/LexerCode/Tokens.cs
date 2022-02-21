namespace bvc2.LexerCode;

abstract record Token(string Text, TokenType Type);

record SymbolToken : Token
{
    public SymbolToken(string text, TokenType tokenType) : base(text, tokenType) { }
}

record StringMarkerToken(TokenType TokenType) : Token("\"", TokenType);

record StringLiteralToken : Token
{
    public StringLiteralToken(string text) : base(text, TokenType.StringLiteral) { }
}

abstract record NumericLiteralToken(string Text, TokenType Type) : Token(Text, Type);
record IntegerLiteralToken(string Text, long Value) : NumericLiteralToken(Text, TokenType.IntegerLiteral)
{
    public IntegerLiteralToken(long Value) : this(Value.ToString(CultureInfo.InvariantCulture), Value) { }
}
record DoubleLiteralToken(string Text, double Value) : NumericLiteralToken(Text, TokenType.DoubleLiteral);

record IdentifierToken : Token
{
    public IdentifierToken(string text) : base(text, TokenType.Identifier) { }
}

record KeywordToken : Token
{
    public KeywordToken(string text, TokenType tokenType) : base(text, tokenType) { }
}