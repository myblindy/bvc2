﻿namespace bvc2.LexerCode;

internal class Lexer
{
    private readonly StreamReader reader;
    private readonly List<Token> previewTokens = new();
    Transaction? transaction;

    public Lexer(Stream stream) =>
        reader = new(stream);

    public class Transaction
    {
        readonly Lexer lexer;
        List<Token>? tokens = new();
        public Transaction(Lexer lexer) => this.lexer = lexer;

        public void Add(Token token) => tokens?.Add(token);
        public void AddRange(IEnumerable<Token> token) => tokens?.AddRange(token);

        public void Reset()
        {
            if (tokens is not null)
                lexer.previewTokens.InsertRange(0, Enumerable.Reverse(tokens));
            Commit();
        }

        public void Commit() => (lexer.transaction, tokens) = (null, null);
    }

    static readonly Dictionary<string, TokenType> keywords = new()
    {
        // ["if"] = TokenType.IfKeyword,
        // ["true"] = TokenType.TrueKeyword,
        // ["false"] = TokenType.FalseKeyword,
        ["var"] = TokenType.VarKeyword,
        // ["val"] = TokenType.ValKeyword,
        // ["enum"] = TokenType.EnumKeyword,
        // ["class"] = TokenType.ClassKeyword,
        // ["fun"] = TokenType.FunKeyword,
        // ["set"] = TokenType.SetKeyword,
        // ["get"] = TokenType.GetKeyword,
        // ["return"] = TokenType.ReturnKeyword,
        // ["vararg"] = TokenType.VarArgKeyword,
        // ["static"] = TokenType.StaticKeyword,
        // ["for"] = TokenType.ForKeyword,
        // ["in"] = TokenType.InKeyword,
    };

    public Transaction? StartTransaction() => transaction is null ? transaction = new(this) : null;

    readonly StringBuilder tokenSB = new();
    readonly Stack<bool> inTokenString = new();
    bool IsInTokenString => inTokenString.TryPeek(out var res) && res;
    bool nextTokenIsStringEnd;
    (long end, bool firstPart)? nextTokenIsRangeEnd;

    public bool PopInStringToken()
    {
        if (inTokenString.Count == 0) throw new NotImplementedException();
        return inTokenString.Pop();
    }

    Token? GetNextToken()
    {
        const char eof = unchecked((char)-1);
        while (true)
        {
            if (nextTokenIsStringEnd)
            {
                if (!inTokenString.Pop()) throw new InvalidOperationException();
                nextTokenIsStringEnd = false;
                return new StringMarkerToken(TokenType.StringEnding);
            }

            if (nextTokenIsRangeEnd is not null)
                if (nextTokenIsRangeEnd.Value.firstPart)
                {
                    nextTokenIsRangeEnd = (nextTokenIsRangeEnd.Value.end, false);
                    return new SymbolToken("..", TokenType.DotDot);
                }
                else
                {
                    (nextTokenIsRangeEnd, var end) = (null, nextTokenIsRangeEnd!.Value.end);
                    return new IntegerLiteralToken(end);
                }

            var ch = (char)reader.Read();

            if (IsInTokenString)
            {
                if (ch is eof or '"')
                {
                    // end of literal token, end of string
                    nextTokenIsStringEnd = true;
                    return new StringLiteralToken("");
                }

                // parse string parts
                tokenSB.Clear();
                tokenSB.Append(ch);

                while (true)
                {
                    ch = (char)reader.Peek();
                    if (ch == '$')
                    {
                        reader.Read();
                        if ((char)reader.Peek() == '{')
                        {
                            // end of the literal token, beginning of the expression
                            reader.Read();
                            inTokenString.Push(false);
                            return new StringLiteralToken(tokenSB.ToString());
                        }
                        tokenSB.Append(ch);
                    }
                    else if (ch is eof or '"')
                    {
                        // end of literal token, end of string
                        nextTokenIsStringEnd = true;
                        reader.Read();
                        return new StringLiteralToken(tokenSB.ToString());
                    }
                    else
                    {
                        tokenSB.Append(ch);
                        reader.Read();
                    }
                }
            }

            if (char.IsWhiteSpace(ch)) continue;
            switch (ch)
            {
                case eof: return null;
                case '"':
                    if (!IsInTokenString)
                    {
                        inTokenString.Push(true);
                        return new StringMarkerToken(TokenType.StringBeginning);
                    }
                    else
                        throw new NotImplementedException();
                case var letterCh when char.IsLetter(ch):
                    {
                        tokenSB.Clear();
                        tokenSB.Append(letterCh);
                        while (true)
                        {
                            ch = (char)reader.Peek();
                            if (ch is eof || !char.IsLetterOrDigit(ch))
                            {
                                var text = tokenSB.ToString();
                                return keywords.TryGetValue(text, out var tokenType) ? new KeywordToken(text, tokenType) : new IdentifierToken(text);
                            }
                            else
                            {
                                tokenSB.Append(ch);
                                reader.Read();
                            }
                        }
                    }
                case '(': return new SymbolToken(ch.ToString(), TokenType.OpenParentheses);
                case ')': return new SymbolToken(ch.ToString(), TokenType.CloseParentheses);
                case '{': return new SymbolToken(ch.ToString(), TokenType.OpenBrace);
                case '}': return new SymbolToken(ch.ToString(), TokenType.CloseBrace);
                case ';': return new SymbolToken(ch.ToString(), TokenType.SemiColon);
                case '+': return new SymbolToken(ch.ToString(), TokenType.Plus);
                case '-': return new SymbolToken(ch.ToString(), TokenType.Minus);
                case '*': return new SymbolToken(ch.ToString(), TokenType.Star);
                case '/': return new SymbolToken(ch.ToString(), TokenType.Slash);
                case ',': return new SymbolToken(ch.ToString(), TokenType.Comma);
                case ':': return new SymbolToken(ch.ToString(), TokenType.Colon);
                case '.':
                    if ((char)reader.Peek() == '.')
                    {
                        reader.Read();
                        return new SymbolToken("..", TokenType.DotDot);
                    }
                    return new SymbolToken(ch.ToString(), TokenType.Dot);
                case '[': return new SymbolToken(ch.ToString(), TokenType.OpenBracket);
                case ']': return new SymbolToken(ch.ToString(), TokenType.CloseBracket);
                case '<' or '>' or '=':
                    {
                        var nextCh = (char)reader.Peek();
                        if (nextCh is not '=')
                            return new SymbolToken(ch.ToString(), ch switch
                            {
                                '<' => TokenType.LessThan,
                                '>' => TokenType.GreaterThan,
                                '=' => TokenType.Equals,
                                _ => throw new NotImplementedException()
                            });
                        else
                        {
                            reader.Read();
                            return new SymbolToken($"{ch}{nextCh}", ch switch
                            {
                                '<' => TokenType.LessThanEqual,
                                '>' => TokenType.GreaterThanEqual,
                                '=' => TokenType.EqualsEquals,
                                _ => throw new NotImplementedException()
                            });
                        }
                    }
                case '!':
                    {
                        var nextCh = reader.Peek();
                        if (nextCh == '=')
                        {
                            reader.Read();
                            return new SymbolToken("!=", TokenType.NotEquals);
                        }
                        else
                            return new SymbolToken("!", TokenType.Not);
                    }
                case >= '0' and <= '9':
                    {
                        tokenSB.Clear();
                        tokenSB.Append(ch);

                        var hasDot = false;

                        while (true)
                        {
                            var nextCh = (char)reader.Peek();

                            if (char.IsDigit(nextCh) || (!hasDot || hasDot && (tokenSB.Length == 1 && tokenSB[^1] == '.' || tokenSB.Length >= 2 && tokenSB[^1] == '.' && tokenSB[^2] != '.')) && nextCh == '.')
                            {
                                reader.Read();
                                tokenSB.Append(nextCh);
                                hasDot |= nextCh == '.';
                            }
                            else
                                break;
                        }

                        var token = tokenSB.ToString();
                        if (token.IndexOf("..", StringComparison.InvariantCulture) is { } dotDotIndex && dotDotIndex >= 0)
                        {
                            nextTokenIsRangeEnd = (int.Parse(token.AsSpan(dotDotIndex + 2)), true);
                            return new IntegerLiteralToken(int.Parse(token.AsSpan(0, dotDotIndex)));
                        }
                        if (hasDot && double.TryParse(token, out var doubleValue))
                            return new DoubleLiteralToken(token, doubleValue);
                        else if (!hasDot && long.TryParse(token, out var intValue))
                            return new IntegerLiteralToken(token, intValue);
                        else
                            throw new NotImplementedException();
                    }
                default: throw new NotImplementedException();
            }
        }
    }

    public Token? Consume(int n = 0)
    {
        if (n >= previewTokens.Count)
        {
            n -= previewTokens.Count;
            transaction?.AddRange(previewTokens);
            previewTokens.Clear();

            Token? token = default;
            while (n-- >= 0) token = GetNextToken();
            return token;
        }
        else
        {
            Token? token = default;
            while (n-- >= 0)
            {
                token = previewTokens[0];
                previewTokens.RemoveAt(0);
                transaction?.Add(token);
            }
            return token;
        }
    }

    public Token? Peek(int n = 0)
    {
        if (n >= previewTokens.Count)
        {
            n -= previewTokens.Count;

            Token? token = default;
            while (n-- >= 0) { token = GetNextToken(); if (token is null) return null; previewTokens.Add(token); }
            return token;
        }
        else if (n == 0)
            return previewTokens[0];
        else
            return previewTokens[n];
    }
}
