using System.Globalization;
using TriUgla.Script.Parsing.Nodes;
using TriUgla.Script.Parsing.Nodes.Expressions;
using TriUgla.Script.Parsing.Nodes.Statements;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public sealed class Parser(Source source)
    {
        readonly Scanner _scanner = new Scanner(source);
        readonly LoopTrack _loops = new LoopTrack();

        public StmtProg Parse()
        {
            return new StmtProg(ParseStatements());
        }

        List<Stmt> ParseStatements(params Keyword[] stops)
        {
            List<Stmt> statements = [];

            while (!IsAtEnd())
            {
                SkipLineBreaks();

                if (IsAtEnd() || CheckKeywordAny(stops))
                    break;

                statements.Add(Statement());

                if (CheckKeywordAny(stops) || IsAtEnd())
                    continue;

                if (!Match(TokenKind.Semicolon) && !Match(TokenKind.LineBreak))
                {
                    statements.Add(StmtErrorAt(
                        Peek(),
                        "Expected ';' or line break after statement."));
                }

                SkipLineBreaks();
            }

            return statements;
        }

        Stmt Statement()
        {
            if (Check(TokenKind.Error))
                return StmtErrorFromToken();

            if (Peek().Kind == TokenKind.Keyword)
            {
                Keyword keyword = (Keyword)Peek().Value;

                return keyword switch
                {
                    Keyword.If => If(),
                    Keyword.For => For(),
                    Keyword.While => While(),
                    Keyword.Break => Break(),
                    Keyword.Continue => Continue(),
                    _ => ExprOrAssign()
                };
            }

            return ExprOrAssign();
        }

        Stmt ExprOrAssign()
        {
            Expr target = Expression();

            if (MatchOp(
                OperatorKind.Assign,
                OperatorKind.PlusAssign,
                OperatorKind.MinusAssign,
                OperatorKind.MultiplyAssign,
                OperatorKind.DivideAssign))
            {
                Token op = Previous;
                Expr value = Expression();

                if (target is not ExprIdentifier and not ExprIndex)
                    return StmtErrorAt(op, "Left side of assignment must be identifier or index.");

                return new StmtAssign(target, op, value);
            }

            return new StmtExpr(target);
        }

        Stmt If()
        {
            ExpectKeyword(Keyword.If, "Expected 'If'.");

            Expr condition = ParenthesizedExpression();

            StmtBlock thenBlock = new(ParseStatements(
                Keyword.ElseIf,
                Keyword.Else,
                Keyword.EndIf));

            List<StmtElseIf> elseIfs = [];

            while (MatchKeyword(Keyword.ElseIf))
            {
                Expr elseIfCondition = ParenthesizedExpression();

                StmtBlock elseIfBlock = new(ParseStatements(
                    Keyword.ElseIf,
                    Keyword.Else,
                    Keyword.EndIf));

                elseIfs.Add(new StmtElseIf(elseIfCondition, elseIfBlock));
            }

            StmtBlock? elseBlock = null;

            if (MatchKeyword(Keyword.Else))
                elseBlock = new StmtBlock(ParseStatements(Keyword.EndIf));

            ExpectKeyword(Keyword.EndIf, "Expected 'EndIf'.");

            return new StmtIf(condition, thenBlock, elseIfs, elseBlock);
        }

        Stmt For()
        {
            ExpectKeyword(Keyword.For, "Expected 'For'.");

            Token variable = Expect(TokenKind.Identifier, "Expected loop variable.");
            ExpectKeyword(Keyword.In, "Expected 'In' after loop variable.");

            Expr iterable = Expression();

            _loops.Enter();

            StmtBlock body = new(ParseStatements(Keyword.EndFor));

            _loops.Exit();

            ExpectKeyword(Keyword.EndFor, "Expected 'EndFor'.");

            return new StmtFor(variable, iterable, body);
        }

        Stmt While()
        {
            ExpectKeyword(Keyword.While, "Expected 'While'.");

            Expr condition = ParenthesizedExpression();

            _loops.Enter();

            StmtBlock body = new(ParseStatements(Keyword.EndWhile));

            _loops.Exit();

            ExpectKeyword(Keyword.EndWhile, "Expected 'EndWhile'.");

            return new StmtWhile(condition, body);
        }

        Stmt Break()
        {
            Token token = ExpectKeyword(Keyword.Break, "Expected 'Break'.");

            if (!_loops.IsInsideLoop)
                return StmtErrorAt(token, "'Break' can only be used inside loop.");

            return new StmtBreak(token);
        }

        Stmt Continue()
        {
            Token token = ExpectKeyword(Keyword.Continue, "Expected 'Continue'.");

            if (!_loops.IsInsideLoop)
                return StmtErrorAt(token, "'Continue' can only be used inside loop.");

            return new StmtContinue(token);
        }

        Expr Expression() => Or();

        Expr Or()
        {
            Expr expr = And();

            while (MatchOp(OperatorKind.Or))
                expr = new ExprBinary(expr, Previous, And());

            return expr;
        }

        Expr And()
        {
            Expr expr = Equality();

            while (MatchOp(OperatorKind.And))
                expr = new ExprBinary(expr, Previous, Equality());

            return expr;
        }

        Expr Equality()
        {
            Expr expr = Comparison();

            while (MatchOp(OperatorKind.Equal, OperatorKind.NotEqual))
                expr = new ExprBinary(expr, Previous, Comparison());

            return expr;
        }

        Expr Comparison()
        {
            Expr expr = Term();

            while (MatchOp(
                OperatorKind.Less,
                OperatorKind.LessEqual,
                OperatorKind.Greater,
                OperatorKind.GreaterEqual))
            {
                expr = new ExprBinary(expr, Previous, Term());
            }

            return expr;
        }

        Expr Term()
        {
            Expr expr = Factor();

            while (MatchOp(OperatorKind.Plus, OperatorKind.Minus))
                expr = new ExprBinary(expr, Previous, Factor());

            return expr;
        }

        Expr Factor()
        {
            Expr expr = Power();

            while (MatchOp(
                OperatorKind.Multiply,
                OperatorKind.Divide,
                OperatorKind.Modulo))
            {
                expr = new ExprBinary(expr, Previous, Power());
            }

            return expr;
        }

        Expr Power()
        {
            Expr expr = Unary();

            if (MatchOp(OperatorKind.Power))
                expr = new ExprBinary(expr, Previous, Power());

            return expr;
        }

        Expr Unary()
        {
            if (MatchOp(
                OperatorKind.Plus,
                OperatorKind.Minus,
                OperatorKind.Not))
            {
                return new ExprUnary(Previous, Unary());
            }

            return Index();
        }

        Expr Index()
        {
            Expr expr = Primary();

            while (Match(TokenKind.OpenSquare))
            {
                Token open = Previous;

                Expr? index = null;

                if (!Check(TokenKind.CloseSquare))
                    index = Expression();

                Token close = Expect(TokenKind.CloseSquare, "Expected closing ']'.");

                expr = new ExprIndex(expr, open, index, close);
            }

            return expr;
        }

        Expr Primary()
        {
            if (Check(TokenKind.Error))
                return ExprErrorFromToken();

            if (Match(TokenKind.Number))
                return Number(Previous);

            if (Match(TokenKind.String))
                return new ExprString(Previous, Previous.Source.Slice(Previous.Span));

            if (Match(TokenKind.Identifier))
                return IdentifierOrCall(Previous);

            if (MatchKeyword(Keyword.True))
                return new ExprBoolean(Previous, true);

            if (MatchKeyword(Keyword.False))
                return new ExprBoolean(Previous, false);

            if (Match(TokenKind.OpenParen))
            {
                Token open = Previous;
                Expr inner = Expression();
                Token close = Expect(TokenKind.CloseParen, "Expected closing ')'.");
                return new ExprGroup(open, inner, close);
            }

            if (Check(TokenKind.OpenCurly))
                return ListOrRange();

            return ExprErrorAt(Peek(), $"Expected expression but got '{Peek().Kind}'.");
        }

        Expr IdentifierOrCall(Token name)
        {
            if (!Match(TokenKind.OpenParen))
                return new ExprIdentifier(name);

            Token open = Previous;
            List<Expr> args = [];

            if (!Check(TokenKind.CloseParen))
            {
                do
                {
                    args.Add(Expression());
                }
                while (Match(TokenKind.Comma));
            }

            Token close = Expect(TokenKind.CloseParen, "Expected ')' after arguments.");

            return new ExprCall(name, open, args, close);
        }

        Expr ListOrRange()
        {
            Token open = Expect(TokenKind.OpenCurly, "Expected '{'.");

            if (Match(TokenKind.CloseCurly))
                return new ExprList(open, [], Previous);

            Expr first = Expression();

            if (Match(TokenKind.Colon))
            {
                Expr end = Expression();
                Expr? step = null;

                if (Match(TokenKind.Colon))
                    step = Expression();

                Expect(TokenKind.CloseCurly, "Expected '}' after range.");

                return new ExprRange(first, end, step);
            }

            List<Expr> items = [first];

            while (Match(TokenKind.Comma))
                items.Add(Expression());

            Token close = Expect(TokenKind.CloseCurly, "Expected '}' after list.");

            return new ExprList(open, items, close);
        }

        ExprNumber Number(Token token)
        {
            string text = token.Source.Slice(token.Span);

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return new ExprNumber(token, i);

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return new ExprNumber(token, d);

            return new ExprNumber(token, double.NaN);
        }

        Expr ParenthesizedExpression()
        {
            Expect(TokenKind.OpenParen, "Expected '('.");
            Expr expr = Expression();
            Expect(TokenKind.CloseParen, "Expected ')'.");
            return expr;
        }

        Expr ExprErrorFromToken()
        {
            Token token = Advance();
            return ExprErrorAt(token, ScanErrorMessage(token));
        }

        Stmt StmtErrorFromToken()
        {
            Token token = Advance();
            return StmtErrorAt(token, ScanErrorMessage(token));
        }

        string ScanErrorMessage(Token token)
        {
            ScanError error = (ScanError)token.Value;

            return error switch
            {
                ScanError.UnterminatedString => "Unterminated string literal.",
                ScanError.InvalidEscape => "Invalid escape sequence.",
                ScanError.InvalidNumber => "Invalid number.",
                ScanError.InvalidOperator => "Invalid operator.",
                ScanError.UnknownCharacter => "Unknown character.",
                ScanError.UnterminatedComment => "Unterminated comment.",
                _ => "Scanner error."
            };
        }

        ExprError ExprErrorAt(Token token, string message)
        {
            Report(token, message);
            Synchronize();
            return new ExprError(token, message);
        }

        StmtError StmtErrorAt(Token token, string message)
        {
            Report(token, message);
            Synchronize();
            return new StmtError(token, message);
        }

        void Report(Token token, string message)
        {
            // Wire your Diagnostics here.
            // Example:
            // diagnostics.Report(token, message, _scanner.LineText(token.Position), _scanner.Marker(token));
        }

        void Synchronize()
        {
            if (IsAtEnd())
                return;

            if (IsHardStop(Peek()) || IsStatementStarter(Peek()))
                return;

            while (!IsAtEnd())
            {
                if (IsHardStop(Peek()) || IsStatementStarter(Peek()))
                    return;

                Advance();
            }
        }

        static bool IsHardStop(Token token)
        {
            return token.Kind is
                TokenKind.Semicolon or
                TokenKind.LineBreak or
                TokenKind.EndOfFile;
        }

        static bool IsStatementStarter(Token token)
        {
            if (token.Kind != TokenKind.Keyword)
                return false;

            Keyword keyword = (Keyword)token.Value;

            return keyword is
                Keyword.If or
                Keyword.For or
                Keyword.While or
                Keyword.Break or
                Keyword.Continue or
                Keyword.ElseIf or
                Keyword.Else or
                Keyword.EndIf or
                Keyword.EndFor or
                Keyword.EndWhile;
        }

        void SkipLineBreaks()
        {
            while (Match(TokenKind.LineBreak))
            {
            }
        }

        Token Peek(int offset = 0) => _scanner.Peek(offset);

        Token Advance() => _scanner.Consume();

        Token Previous => _scanner.Previous;

        bool IsAtEnd() => Peek().Kind == TokenKind.EndOfFile;

        bool Check(TokenKind kind) => Peek().Kind == kind;

        bool Match(params TokenKind[] kinds)
        {
            TokenKind current = Peek().Kind;

            for (int i = 0; i < kinds.Length; i++)
            {
                if (current == kinds[i])
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        bool MatchOp(params OperatorKind[] operators)
        {
            Token token = Peek();

            if (token.Kind != TokenKind.Operator)
                return false;

            OperatorKind current = (OperatorKind)token.Value;

            for (int i = 0; i < operators.Length; i++)
            {
                if (current == operators[i])
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        bool MatchKeyword(Keyword keyword)
        {
            Token token = Peek();

            if (token.Kind != TokenKind.Keyword)
                return false;

            if ((Keyword)token.Value != keyword)
                return false;

            Advance();
            return true;
        }

        bool CheckKeywordAny(params Keyword[] keywords)
        {
            Token token = Peek();

            if (token.Kind != TokenKind.Keyword)
                return false;

            Keyword current = (Keyword)token.Value;

            for (int i = 0; i < keywords.Length; i++)
                if (current == keywords[i])
                    return true;

            return false;
        }

        Token Expect(TokenKind kind, string message)
        {
            if (Check(kind))
                return Advance();

            Report(Peek(), message);
            Synchronize();

            return Peek();
        }

        Token ExpectKeyword(Keyword keyword, string message)
        {
            if (MatchKeyword(keyword))
                return Previous;

            Report(Peek(), message);
            Synchronize();

            return Peek();
        }
    }
}
