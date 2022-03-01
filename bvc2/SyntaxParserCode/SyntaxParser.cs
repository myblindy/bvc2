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

    static VariableModifiers BuildVariableModifiers(bool @static, bool val) =>
        (@static ? VariableModifiers.Static : VariableModifiers.None) | (val ? VariableModifiers.Val : VariableModifiers.None);

    public RootSyntaxNode Parse()
    {
        var root = new RootSyntaxNode();
        ParseChildren(root, ParseContextType.Class);
        return root;
    }

    enum ParseContextType { Function, Class }
    void ParseChildren(ParentSyntaxNode node, ParseContextType parseContextType, bool onlyOne = false)
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

                        enumNode.Children.Add(new VariableSyntaxNode(VariableModifiers.Enum, identifier.Text,
                            new IdentifierExpressionSyntaxNode(enumName), new LiteralExpressionSyntaxNode(nextValue - 1)));

                        if (MatchTokenTypes(TokenType.Comma) is null)
                            break;
                    }

                    node.Children.Add(enumNode);
                }

            // class
            // class X { }
            // class X(var v: int, var j: int) { }
            if (parseContextType is ParseContextType.Class)
                while (MatchTokenTypes(TokenType.ClassKeyword) is { })
                {
                    var name = ExpectTokenTypes(TokenType.Identifier).Text;
                    var classDeclarationNode = new ClassSyntaxNode(name);

                    // primary constructor
                    if (MatchTokenTypes(TokenType.OpenParentheses) is { })
                    {
                        var args = new List<(VariableModifiers Modifiers, string Name, IdentifierExpressionSyntaxNode Type)>();
                        while (true)
                        {
                            if (args.Count > 0 && MatchTokenTypes(TokenType.Comma) is null)
                                break;

                            if (MatchTokenTypes(TokenType.ValKeyword, TokenType.VarKeyword) is not { } varTypeToken)
                                break;

                            var varName = ExpectTokenTypes(TokenType.Identifier).Text;
                            ExpectTokenTypes(TokenType.Colon);
                            var type = ParseIdentifierExpressionSyntaxNode()!;

                            args.Add((BuildVariableModifiers(false, varTypeToken.Type is TokenType.ValKeyword), varName, type));
                        }
                        ExpectTokenTypes(TokenType.CloseParentheses);

                        classDeclarationNode.Children.Add(new FunctionDeclarationSyntaxNode(FunctionModifiers.None, FunctionDeclarationSyntaxNode.PrimaryConstructorName, null, args.ToArray()));
                    }

                    if (MatchTokenTypes(TokenType.OpenBrace) is { })
                    {
                        ParseChildren(classDeclarationNode, ParseContextType.Class);
                        ExpectTokenTypes(TokenType.CloseBrace);
                    }
                    else
                        ExpectTokenTypes(TokenType.SemiColon);

                    node.Children.Add(classDeclarationNode);
                    if (onlyOne) return;
                    foundAny = true;
                }

            // variables
            // var/val a: A;
            while (MatchTokenTypes(TokenType.VarKeyword, TokenType.ValKeyword) is { } variableKindToken)
            {
                var name = ExpectTokenTypes(TokenType.Identifier).Text;
                IdentifierExpressionSyntaxNode? type = null;
                if (MatchTokenTypes(TokenType.Colon) is { })
                    type = (IdentifierExpressionSyntaxNode)ParseExpression()!;

                ExpressionSyntaxNode? initialValue = default;
                bool initialValueIsGet = false, needsSemiColon = true;
                FunctionDeclarationSyntaxNode? functionGet = default;
                if (MatchTokenTypes(TokenType.GetKeyword) is { })
                {
                    if (MatchTokenTypes(TokenType.Equals) is { })
                        (initialValue, initialValueIsGet) = (ParseExpression(), true);
                    else if (MatchTokenTypes(TokenType.OpenBrace) is { })
                    {
                        ParseChildren(functionGet = new(FunctionModifiers.None, $"get_{name}", type, Array.Empty<(VariableModifiers, string, IdentifierExpressionSyntaxNode)>(), true), ParseContextType.Function);
                        ExpectTokenTypes(TokenType.CloseBrace);
                        needsSemiColon = false;
                    }
                    else
                        throw new NotImplementedException();
                }

                if (initialValue is null && MatchTokenTypes(TokenType.Equals) is { })
                    initialValue = ParseExpression();

                if (initialValue is null && type is null && functionGet is null)
                    throw new NotImplementedException();

                if (needsSemiColon)
                    ExpectTokenTypes(TokenType.SemiColon);

                foundAny = true;
                if (functionGet is null && initialValue is not null && initialValueIsGet)
                    functionGet = new(FunctionModifiers.None, $"get_{name}", type, Array.Empty<(VariableModifiers, string, IdentifierExpressionSyntaxNode)>(), true);

                if (functionGet?.Children.Count == 0)
                    functionGet.Children.Add(new ReturnStatementNode(initialValue!));
                if (functionGet is not null)
                    node.Children.Add(functionGet);

                node.Children.Add(new VariableSyntaxNode(BuildVariableModifiers(false, variableKindToken.Type is TokenType.ValKeyword),
                    name, type, initialValueIsGet ? null : initialValue, functionGet));
                if (onlyOne) return;
            }
        } while (foundAny);
    }

    ExpressionSyntaxNode? ParseExpression() => ParseEqualityExpression();

    ExpressionSyntaxNode? ParseEqualityExpression()
    {
        var left = ParseComparisonExpression();
        if (left is null) return null;

        while (MatchTokenTypes(TokenType.EqualsEquals, TokenType.NotEquals) is { } operatorToken)
        {
            var right = ParseComparisonExpression();
            if (right is null) throw new NotImplementedException();

            left = new BinaryExpressionSyntaxNode(left, operatorToken.Type, right);
        }

        return left;
    }

    ExpressionSyntaxNode? ParseComparisonExpression()
    {
        var left = ParseTermExpression();
        if (left is null) return null;

        while (MatchTokenTypes(TokenType.LessThan, TokenType.LessThanEqual, TokenType.GreaterThan, TokenType.GreaterThanEqual) is { } operatorToken)
        {
            var right = ParseTermExpression();
            if (right is null) throw new NotImplementedException();

            left = new BinaryExpressionSyntaxNode(left, operatorToken.Type, right);
        }

        return left;
    }

    ExpressionSyntaxNode? ParseTermExpression()
    {
        var left = ParseFactorExpression();
        if (left is null) return null;

        while (MatchTokenTypes(TokenType.Plus, TokenType.Minus) is { } operatorToken)
        {
            var right = ParseFactorExpression();
            if (right is null) throw new NotImplementedException();

            left = new BinaryExpressionSyntaxNode(left, operatorToken.Type, right);
        }

        return left;
    }

    ExpressionSyntaxNode? ParseFactorExpression()
    {
        var left = ParseUnaryExpression();
        if (left is null) return null;

        while (MatchTokenTypes(TokenType.Star, TokenType.Slash) is { } operatorToken)
        {
            var right = ParseUnaryExpression();
            if (right is null) throw new NotImplementedException();

            left = new BinaryExpressionSyntaxNode(left, operatorToken.Type, right);
        }

        return left;
    }

    ExpressionSyntaxNode? ParseUnaryExpression()
    {
        if (MatchTokenTypes(TokenType.Not, TokenType.Minus) is { } operatorToken)
        {
            var right = ParsePrimaryExpression();
            if (right is null) throw new NotImplementedException();

            return new UnaryExpressionSyntaxNode(operatorToken.Type, right);
        }

        return ParseRangeExpression();
    }

    ExpressionSyntaxNode? ParseRangeExpression()
    {
        var left = ParsePrimaryExpression();
        if (left is null) return null;

        while (MatchTokenTypes(TokenType.DotDot) is { })
        {
            var right = ParsePrimaryExpression();
            if (right is null) throw new NotImplementedException();

            left = new FunctionCallExpressionSyntaxNode(new IdentifierExpressionSyntaxNode("Range"), new[] { left, right });
        }

        return left;
    }

    ExpressionSyntaxNode? ParsePrimaryExpression()
    {
        if (MatchTokenTypes(TokenType.TrueKeyword, TokenType.FalseKeyword) is { } operatorToken)
            return new LiteralExpressionSyntaxNode(operatorToken.Type is TokenType.TrueKeyword);
        else if (MatchTokenTypes(TokenType.StringBeginning) is { })
        {
            var expressions = new List<ExpressionSyntaxNode>();
            while (true)
                if (MatchTokenTypes(TokenType.StringEnding) is { })
                {
                    if (expressions.Count == 1 && expressions[0] is LiteralExpressionSyntaxNode literalExpressionSyntaxNode)
                        return literalExpressionSyntaxNode;
                    var usefulCheck = (ExpressionSyntaxNode e) => !(e is LiteralExpressionSyntaxNode l && l.Value is string s && string.IsNullOrEmpty(s));
                    var usefulCount = expressions.Count(usefulCheck);
                    return usefulCount == 0 ? new LiteralExpressionSyntaxNode("") : new StringExpressionSyntaxNode(expressions.Where(usefulCheck).ToArray());
                }
                else if (MatchTokenTypes(TokenType.StringLiteral) is { } stringLiteralToken)
                    expressions.Add(new LiteralExpressionSyntaxNode(stringLiteralToken.Text));
                else if (ParseExpression() is { } expression)
                {
                    expressions.Add(expression);
                    ExpectTokenTypes(TokenType.CloseBrace);
                    if (lexer.PopInStringToken()) throw new NotImplementedException();
                }
        }
        else if (MatchTokenTypes(TokenType.StringLiteral) is { } stringLiteralToken)
            return new LiteralExpressionSyntaxNode(stringLiteralToken.Text);
        else if (MatchTokenTypes(TokenType.IntegerLiteral) is IntegerLiteralToken integerLiteralToken)
            return new LiteralExpressionSyntaxNode(integerLiteralToken.Value);
        else if (MatchTokenTypes(TokenType.DoubleLiteral) is DoubleLiteralToken doubleLiteralToken)
            return new LiteralExpressionSyntaxNode(doubleLiteralToken.Value);
        else if (MatchTokenTypes(TokenType.OpenParentheses) is { })
        {
            var expr = ParseExpression();
            if (expr is null || lexer.Consume()!.Type != TokenType.CloseParentheses) throw new NotImplementedException();

            return new GroupingExpressionSyntaxNode(expr);
        }
        else if (MatchTokenTypes(TokenType.Identifier) is { } identifierToken)
        {
            ExpressionSyntaxNode node = ParseIdentifierExpressionSyntaxNode(identifierToken)!;
            return decorateNode(node);
        }
        else if (MatchTokenTypes(TokenType.OpenBracket) is { })
        {
            var arguments = new List<ExpressionSyntaxNode>();
            while (true)
            {
                if (arguments.Count > 0 && MatchTokenTypes(TokenType.Comma) is null)
                    break;

                var expr = ParseExpression();
                if (expr is null) throw new NotImplementedException();
                arguments.Add(expr);
            }
            ExpectTokenTypes(TokenType.CloseBracket);
            return decorateNode(new FunctionCallExpressionSyntaxNode(new IdentifierExpressionSyntaxNode("List"), arguments.ToArray()));
        }

        ExpressionSyntaxNode decorateNode(ExpressionSyntaxNode node)
        {
            var oldNode = node;

            do
            {
                oldNode = node;

                while (MatchTokenTypes(TokenType.Dot) is { })
                    node = new BinaryExpressionSyntaxNode(node, TokenType.Dot, new IdentifierExpressionSyntaxNode(ExpectTokenTypes(TokenType.Identifier).Text));

                while (MatchTokenTypes(TokenType.OpenParentheses) is { })
                {
                    // function call
                    var arguments = new List<ExpressionSyntaxNode>();
                    if (MatchTokenTypes(TokenType.CloseParentheses) is null)
                    {
                        while (true)
                        {
                            if (arguments.Count > 0 && MatchTokenTypes(TokenType.Comma) is null)
                                break;

                            var expr = ParseExpression();
                            if (expr is null) throw new NotImplementedException();
                            arguments.Add(expr);
                        }
                        ExpectTokenTypes(TokenType.CloseParentheses);
                    }
                    node = new FunctionCallExpressionSyntaxNode(node, arguments.ToArray());
                }

                while (MatchTokenTypes(TokenType.OpenBracket) is { })
                {
                    // index call
                    var arguments = new List<ExpressionSyntaxNode>();
                    if (MatchTokenTypes(TokenType.CloseBracket) is null)
                    {
                        while (true)
                        {
                            if (arguments.Count > 0 && MatchTokenTypes(TokenType.Comma) is null)
                                break;

                            var expr = ParseExpression();
                            if (expr is null) throw new NotImplementedException();
                            arguments.Add(expr);
                        }
                        ExpectTokenTypes(TokenType.CloseBracket);
                    }
                    node = new FunctionCallExpressionSyntaxNode(new BinaryExpressionSyntaxNode(node, TokenType.Dot, new IdentifierExpressionSyntaxNode("Get")), arguments.ToArray());
                }
            } while (node != oldNode);

            return node;
        }

        return null;
    }

    IdentifierExpressionSyntaxNode? ParseIdentifierExpressionSyntaxNode(Token? parsedToken = null)
    {
        if (parsedToken is null)
            if (MatchTokenTypes(TokenType.Identifier) is not { } matchedToken)
                return null;
            else
                parsedToken ??= matchedToken;

        var id = parsedToken.Text;
        var genericExpressions = new List<ExpressionSyntaxNode>();

        // start a transaction at <
        var transaction = lexer.StartTransaction();
        try
        {
            // try to match a generic list
            if (MatchTokenTypes(TokenType.LessThan) is { })
            {
                var expr = ParsePrimaryExpression();
                if (expr is null) return new(id);
                genericExpressions.Add(expr);

                while (MatchTokenTypes(TokenType.Comma) is { })
                {
                    expr = ParsePrimaryExpression();
                    if (expr is null) return new(id);
                    genericExpressions.Add(expr);
                }

                if (MatchTokenTypes(TokenType.GreaterThan) is { })
                {
                    // good case
                    transaction?.Commit();
                    return new(id, genericExpressions.ToArray());
                }
            }
        }
        finally
        {
            transaction?.Reset();
        }

        return new(id);
    }
}
