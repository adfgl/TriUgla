using System.Globalization;

namespace TriUgla.Script;

public sealed class Parser
{
    readonly TokenReader _tokens;
    readonly DiagnosticBag _diagnostics = new();
    bool _errorInStatement;
    int _blockDepth;

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Items;

    public Parser(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _tokens = new TokenReader(source);
    }

    public CompilationUnit ParseCompilationUnit()
    {
        var statements = new List<Stmt>();

        while (Peek().Kind != TokenKind.EndOfFile)
        {
            int start = _tokens.Position;
            statements.Add(ParseStatement());

            if (_tokens.Position == start)
            {
                Read();
            }
        }

        return new CompilationUnit(statements, Read());
    }

    Stmt ParseStatement()
    {
        _errorInStatement = false;

        if (IsKeyword(KeywordKind.If))
        {
            return ParseIfStatement();
        }

        if (IsKeyword(KeywordKind.For))
        {
            return ParseForStatement();
        }

        if (Peek().Kind == TokenKind.LeftBrace)
        {
            return ParseBlock();
        }

        Expr target = ParseExpression();
        Stmt statement;

        if (Peek().Kind == TokenKind.Equals)
        {
            Token equals = Read();
            Expr value = ParseExpression();
            Token semicolon = ReadStatementEnd(value.Span);
            statement = new AssignmentStmt(target, equals, value, semicolon);
        }
        else
        {
            Token semicolon = ReadStatementEnd(target.Span);
            statement = new ExpressionStmt(target, semicolon);
        }

        return statement;
    }

    IfStmt ParseIfStatement()
    {
        var branches = new List<ConditionalBranch>();
        Token ifKeyword = Read();
        branches.Add(ParseConditionalBranch(ifKeyword));

        while (IsKeyword(KeywordKind.ElseIf))
        {
            Token elseIfKeyword = Read();
            branches.Add(ParseConditionalBranch(elseIfKeyword));
        }

        if (IsKeyword(KeywordKind.Else))
        {
            Token elseKeyword = Read();
            branches.Add(new ConditionalBranch(
                elseKeyword,
                null,
                ParseStatementsUntil(KeywordKind.EndIf)));
        }

        _errorInStatement = false;
        Token endIf = ExpectKeyword(
            KeywordKind.EndIf,
            "TS1008",
            "Expected 'EndIf' to close conditional statement.");
        return new IfStmt(branches, endIf);
    }

    ForStmt ParseForStatement()
    {
        Token forKeyword = Read();
        Token? iterator = null;
        TokenKind closingToken;

        if (Peek().Kind == TokenKind.LeftParenthesis)
        {
            Read();
            closingToken = TokenKind.RightParenthesis;
        }
        else
        {
            iterator = Expect(
                TokenKind.Identifier,
                "TS1011",
                "Expected iterator name or '(' after 'For'.");
            ExpectKeyword(KeywordKind.In, "TS1012", "Expected 'In' after loop iterator.");
            Expect(TokenKind.LeftBrace, "TS1013", "Expected '{' before iterator range.");
            closingToken = TokenKind.RightBrace;
        }

        Expr start = ParseExpression();
        Expect(TokenKind.Colon, "TS1014", "Expected ':' after loop range start.");
        Expr end = ParseExpression();
        Expr? step = null;
        if (Peek().Kind == TokenKind.Colon)
        {
            Read();
            step = ParseExpression();
        }

        Expect(closingToken, "TS1015", "Expected closing delimiter after loop range.");
        IReadOnlyList<Stmt> statements = ParseStatementsUntil(KeywordKind.EndFor);
        _errorInStatement = false;
        Token endFor = ExpectKeyword(
            KeywordKind.EndFor,
            "TS1016",
            "Expected 'EndFor' to close loop statement.");
        return new ForStmt(forKeyword, iterator, start, end, step, statements, endFor);
    }

    ConditionalBranch ParseConditionalBranch(Token keyword)
    {
        Expect(TokenKind.LeftParenthesis, "TS1009", $"Expected '(' after '{keyword.Text}'.");
        Expr condition = ParseExpression();
        Expect(TokenKind.RightParenthesis, "TS1010", "Expected ')' after conditional expression.");
        IReadOnlyList<Stmt> statements = ParseStatementsUntil(
            KeywordKind.ElseIf,
            KeywordKind.Else,
            KeywordKind.EndIf);
        return new ConditionalBranch(keyword, condition, statements);
    }

    IReadOnlyList<Stmt> ParseStatementsUntil(params KeywordKind[] terminators)
    {
        var statements = new List<Stmt>();
        while (Peek().Kind != TokenKind.EndOfFile &&
               !terminators.Any(IsKeyword))
        {
            int start = _tokens.Position;
            statements.Add(ParseStatement());
            if (_tokens.Position == start)
            {
                Read();
            }
        }

        return statements;
    }

    BlockStmt ParseBlock()
    {
        Token leftBrace = Read();
        var statements = new List<Stmt>();
        _blockDepth++;

        while (Peek().Kind is not TokenKind.RightBrace and not TokenKind.EndOfFile)
        {
            int start = _tokens.Position;
            statements.Add(ParseStatement());
            if (_tokens.Position == start)
            {
                Read();
            }
        }

        _blockDepth--;
        Token rightBrace = Expect(TokenKind.RightBrace, "TS1004", "Expected '}' to close block.");
        return new BlockStmt(leftBrace, statements, rightBrace);
    }

    Expr ParseExpression(int parentPrecedence = 0)
    {
        Expr left;
        int unaryPrecedence = UnaryPrecedence(Peek().Kind);

        if (unaryPrecedence > 0 && unaryPrecedence >= parentPrecedence)
        {
            Token op = Read();
            left = new UnaryExpr(op, ParseExpression(unaryPrecedence));
        }
        else
        {
            left = ParsePostfix();
        }

        while (true)
        {
            int precedence = BinaryPrecedence(Peek().Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            Token op = Read();
            Expr right = ParseExpression(precedence);
            left = new BinaryExpr(left, op, right);
        }

        return left;
    }

    Expr ParsePostfix()
    {
        Expr expression = ParsePrimary();

        while (Peek().Kind == TokenKind.LeftParenthesis)
        {
            Token leftParenthesis = Read();
            IReadOnlyList<Expr> arguments = ParseSeparatedExpressions(TokenKind.RightParenthesis);
            Token rightParenthesis = Expect(
                TokenKind.RightParenthesis,
                "TS1002",
                "Expected ')' after arguments.");
            expression = new CallExpr(expression, leftParenthesis, arguments, rightParenthesis);
        }

        return expression;
    }

    Expr ParsePrimary()
    {
        Token token = Peek();

        switch (token.Kind)
        {
            case TokenKind.Identifier:
                return new NameExpr(Read());

            case TokenKind.Number:
                Read();
                if (double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                {
                    return new LiteralExpr(token, number);
                }

                ReportError("TS1005", $"Invalid number '{token.Text}'.", token.Span);
                return new ErrorExpr(token);

            case TokenKind.String:
                Read();
                return new LiteralExpr(token, DecodeString(token.Text));

            case TokenKind.LeftParenthesis:
                Token leftParenthesis = Read();
                Expr expression = ParseExpression();
                Token rightParenthesis = Expect(
                    TokenKind.RightParenthesis,
                    "TS1001",
                    "Expected ')' after expression.");
                return new GroupExpr(leftParenthesis, expression, rightParenthesis);

            case TokenKind.LeftBrace:
                return ParseList();

            default:
                ReportError("TS1000", $"Expected expression, found '{Display(token)}'.", token.Span);
                if (token.Kind != TokenKind.EndOfFile)
                {
                    Read();
                }

                return new ErrorExpr(token);
        }
    }

    ListExpr ParseList()
    {
        Token leftBrace = Read();
        IReadOnlyList<Expr> items = ParseSeparatedExpressions(TokenKind.RightBrace);
        Token rightBrace = Expect(TokenKind.RightBrace, "TS1003", "Expected '}' after list.");
        return new ListExpr(leftBrace, items, rightBrace);
    }

    IReadOnlyList<Expr> ParseSeparatedExpressions(TokenKind end)
    {
        var expressions = new List<Expr>();

        while (Peek().Kind != end && Peek().Kind != TokenKind.EndOfFile)
        {
            expressions.Add(ParseExpression());
            if (Peek().Kind != TokenKind.Comma)
            {
                break;
            }

            Read();
        }

        return expressions;
    }

    Token ReadStatementEnd(TextSpan preceding)
    {
        if (Peek().Kind == TokenKind.Semicolon)
        {
            return Read();
        }

        Token next = Peek();
        ReportError("TS1006", "Expected ';' after statement.", next.Span);
        Token missing = new(
            TokenKind.Semicolon,
            string.Empty,
            new TextSpan(preceding.End, 0, preceding.Line, preceding.Column + preceding.Length));

        if (next.Kind == TokenKind.Identifier && next.Span.Line > preceding.Line)
        {
            return missing;
        }

        SynchronizeStatement();
        return missing;
    }

    void SynchronizeStatement()
    {
        while (Peek().Kind is not TokenKind.Semicolon and not TokenKind.EndOfFile)
        {
            if (_blockDepth > 0 && Peek().Kind == TokenKind.RightBrace)
            {
                break;
            }

            Read();
        }

        if (Peek().Kind == TokenKind.Semicolon)
        {
            Read();
        }
    }

    Token Expect(TokenKind kind, string code, string message)
    {
        if (Peek().Kind == kind)
        {
            return Read();
        }

        Token current = Peek();
        ReportError(code, message, current.Span);
        return new Token(kind, string.Empty, new TextSpan(
            current.Span.Start,
            0,
            current.Span.Line,
            current.Span.Column));
    }

    Token ExpectKeyword(KeywordKind keyword, string code, string message)
    {
        if (IsKeyword(keyword))
        {
            return Read();
        }

        Token current = Peek();
        ReportError(code, message, current.Span);
        return new Token(
            TokenKind.Keyword,
            string.Empty,
            new TextSpan(current.Span.Start, 0, current.Span.Line, current.Span.Column),
            keyword);
    }

    void ReportError(string code, string message, TextSpan span)
    {
        if (_errorInStatement)
        {
            return;
        }

        _diagnostics.Error(code, message, span);
        _errorInStatement = true;
    }

    Token Peek() => _tokens.Peek();

    Token Read() => _tokens.Read();

    bool IsKeyword(KeywordKind keyword)
        => Peek().Kind == TokenKind.Keyword && Peek().Keyword == keyword;

    static string Display(Token token)
        => token.Kind == TokenKind.EndOfFile ? "end of file" : token.Text;

    static string DecodeString(string text)
        => text.Length < 2
            ? string.Empty
            : text[1..^1]
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");

    static int UnaryPrecedence(TokenKind kind)
        => kind is TokenKind.Plus or TokenKind.Minus or TokenKind.Bang ? 7 : 0;

    static int BinaryPrecedence(TokenKind kind)
        => kind switch
        {
            TokenKind.Star or TokenKind.Slash or TokenKind.Percent => 6,
            TokenKind.Plus or TokenKind.Minus => 5,
            TokenKind.Less or TokenKind.LessOrEquals or
                TokenKind.Greater or TokenKind.GreaterOrEquals => 4,
            TokenKind.EqualsEquals or TokenKind.BangEquals => 3,
            _ => 0
        };
}
