namespace bvc2.LexerCode;

enum TokenType
{
    None,

    StringLiteral,
    IntegerLiteral,
    DoubleLiteral,
    Identifier,

    StringBeginning,
    StringEnding,

    OpenParentheses,
    CloseParentheses,
    OpenBracket,
    CloseBracket,
    OpenBrace,
    CloseBrace,
    SemiColon,
    Colon,
    Plus,
    Minus,
    Star,
    Slash,
    Comma,
    Equals,
    NotEquals,
    LessThan,
    LessThanEqual,
    GreaterThan,
    GreaterThanEqual,
    EqualsEquals,
    Not,
    Dot,
    DotDot,

    VarKeyword,
    EnumKeyword,
}
