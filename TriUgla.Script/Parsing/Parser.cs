using System.Globalization;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public sealed class Parser(Source source, Diagnostics diagnostics)
    {
        readonly Scanner _scanner = new Scanner(source);
        readonly Diagnostics _diagnostics = diagnostics;
        readonly LoopTrack _loops = new LoopTrack();

        public StmtProg Parse()
            => new(ParseStatements());

        void Report(Token token, string message)
        {
            _diagnostics.Error(message, token);
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

                if (IsAtEnd() || CheckKeywordAny(stops))
                    continue;

                if (!Match(TokenKind.Semicolon) && !Match(TokenKind.LineBreak))
                    statements.Add(StmtErrorAt(Peek(), "Expected ';' or line break after statement."));

                SkipLineBreaks();
            }

            return statements;
        }

        Stmt Statement()
        {
            if (Check(TokenKind.Error))
                return StmtErrorFromToken();

            if (CheckKeyword(Keyword.If)) return If();
            if (CheckKeyword(Keyword.For)) return For();
            if (CheckKeyword(Keyword.While)) return While();
            if (CheckKeyword(Keyword.Break)) return Break();
            if (CheckKeyword(Keyword.Continue)) return Continue();

            if (CheckKeyword(Keyword.Point)) return Point();

            if (CheckKeyword(Keyword.Line) && Peek(1).Kind == TokenKind.OpenParen) return Line();
            if (CheckKeyword(Keyword.Line) && Peek(1).Is(Keyword.Loop)) return CurveLoop();

            if (CheckKeyword(Keyword.Circle)) return Circle();
            if (CheckKeyword(Keyword.Ellipse)) return Ellipse();
            if (CheckKeyword(Keyword.Spline)) return Spline();
            if (CheckKeyword(Keyword.BSpline)) return BSpline();
            if (CheckKeyword(Keyword.Bezier)) return Bezier();

            if (CheckKeyword(Keyword.Curve) && Peek(1).Is(Keyword.Loop)) return CurveLoop();

            if (CheckKeyword(Keyword.Plane)) return PlaneSurface();
            if (CheckKeyword(Keyword.Physical)) return Physical();

            if (CheckKeyword(Keyword.Transfinite)) return Transfinite();
            if (CheckKeyword(Keyword.Recombine)) return Recombine();

            if (IsEmbedStart()) return Embed();

            if (CheckKeyword(Keyword.Mesh) && Peek(1).Kind == TokenKind.Dot) return MeshOption();
            if (CheckKeyword(Keyword.Mesh)) return MeshCommand();

            if (IsCommandStart()) return MeshCommand();

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

                if (target is not ExprIdentifier and not ExprIndex and not ExprMember)
                    return StmtErrorAt(op, "Left side of assignment must be identifier, index, or member.");

                return new StmtAssign(target, op, value);
            }

            return new StmtExpr(target);
        }

        Stmt Point() => Entity1(Keyword.Point, (id, values) => new StmtPoint(id, values));
        Stmt Line() => Entity1(Keyword.Line, (id, values) => new StmtLine(id, values));
        Stmt Circle() => Entity1(Keyword.Circle, (id, values) => new StmtCircle(id, values));
        Stmt Ellipse() => Entity1(Keyword.Ellipse, (id, values) => new StmtEllipse(id, values));
        Stmt Spline() => Entity1(Keyword.Spline, (id, values) => new StmtSpline(id, values));
        Stmt BSpline() => Entity1(Keyword.BSpline, (id, values) => new StmtBSpline(id, values));
        Stmt Bezier() => Entity1(Keyword.Bezier, (id, values) => new StmtBezier(id, values));

        Stmt Entity1(Keyword keyword, Func<Expr, Expr, Stmt> make)
        {
            ExpectKeyword(keyword, $"Expected '{keyword}'.");
            Expr id = ParenthesizedExpression();
            ExpectOp(OperatorKind.Assign, $"Expected '=' after {keyword}(...).");
            Expr values = ListExpression();
            return make(id, values);
        }

        Stmt CurveLoop()
        {
            if (MatchKeyword(Keyword.Line))
                ExpectKeyword(Keyword.Loop, "Expected 'Loop' after 'Line'.");
            else
            {
                ExpectKeyword(Keyword.Curve, "Expected 'Curve'.");
                ExpectKeyword(Keyword.Loop, "Expected 'Loop' after 'Curve'.");
            }

            Expr id = ParenthesizedExpression();
            ExpectOp(OperatorKind.Assign, "Expected '=' after Curve Loop(...).");
            Expr values = ListExpression();

            return new StmtCurveLoop(id, values);
        }

        Stmt PlaneSurface()
        {
            ExpectKeyword(Keyword.Plane, "Expected 'Plane'.");
            ExpectKeyword(Keyword.Surface, "Expected 'Surface' after 'Plane'.");

            Expr id = ParenthesizedExpression();
            ExpectOp(OperatorKind.Assign, "Expected '=' after Plane Surface(...).");
            Expr values = ListExpression();

            return new StmtPlaneSurface(id, values);
        }

        Stmt Transfinite()
        {
            ExpectKeyword(Keyword.Transfinite, "Expected 'Transfinite'.");

            if (MatchKeyword(Keyword.Curve))
            {
                Expr curves = ListExpression();
                ExpectOp(OperatorKind.Assign, "Expected '=' after Transfinite Curve{...}.");

                Expr divisions = Expression();

                Expr progression = new ExprNumber(default, 1);

                if (MatchKeyword(Keyword.Using))
                {
                    ExpectKeyword(Keyword.Progression, "Expected 'Progression'.");
                    progression = Expression();
                }

                return new StmtTransfiniteCurve(curves, divisions, progression);
            }

            if (MatchKeyword(Keyword.Surface))
            {
                Expr surfaces = ListExpression();
                Expr? corners = null;

                if (MatchOp(OperatorKind.Assign))
                {
                    corners = ListExpression();
                }

                return new StmtTransfiniteSurface(
                    surfaces,
                    corners);
            }

            return StmtErrorAt(Peek(), "Expected 'Curve' or 'Surface' after 'Transfinite'.");
        }

        Stmt Recombine()
        {
            ExpectKeyword(Keyword.Recombine, "Expected 'Recombine'.");
            ExpectKeyword(Keyword.Surface, "Expected 'Surface' after 'Recombine'.");

            Expr surfaces = ListExpression();

            return new StmtRecombineSurface(surfaces);
        }

        Stmt Physical()
        {
            ExpectKeyword(Keyword.Physical, "Expected 'Physical'.");

            Keyword kind;

            if (MatchKeyword(Keyword.Point)) kind = Keyword.Point;
            else if (MatchKeyword(Keyword.Curve) || MatchKeyword(Keyword.Line)) kind = Keyword.Curve;
            else if (MatchKeyword(Keyword.Surface)) kind = Keyword.Surface;
            else return StmtErrorAt(Peek(), "Expected Point, Curve, Line, or Surface after Physical.");

            Expr id = ParenthesizedExpression();
            ExpectOp(OperatorKind.Assign, "Expected '=' after Physical group.");
            Expr values = ListExpression();

            return new StmtPhysical(kind, id, values, null);
        }

        Stmt Embed()
        {
            Keyword entity = EntityKindKeyword();
            Expr entities = ListExpression();

            ExpectKeyword(Keyword.In, "Expected 'In'.");

            Keyword container = EntityKindKeyword();
            Expr containers = ListExpression();

            return new StmtEmbed(entity, entities, container, containers);
        }

        Stmt MeshOption()
        {
            List<Token> path = ReadPath();
            ExpectOp(OperatorKind.Assign, "Expected '=' after mesh option.");
            Expr value = Expression();

            return new StmtMeshOption(path, value);
        }

        Stmt MeshCommand()
        {
            List<Token> path = ReadCommandPath();
            List<Expr> args = [];

            while (!IsAtEnd() &&
                   !Check(TokenKind.Semicolon) &&
                   !Check(TokenKind.LineBreak))
            {
                args.Add(Expression());
            }

            return new StmtMeshCommand(path, args);
        }

        Stmt If()
        {
            ExpectKeyword(Keyword.If, "Expected 'If'.");
            Expr condition = ParenthesizedExpression();

            StmtBlock thenBranch = new(ParseStatements(
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

            Stmt? elseBranch = null;

            if (MatchKeyword(Keyword.Else))
                elseBranch = new StmtBlock(ParseStatements(Keyword.EndIf));

            ExpectKeyword(Keyword.EndIf, "Expected 'EndIf'.");

            return new StmtIf(condition, thenBranch, elseIfs, elseBranch);
        }

        Stmt For()
        {
            ExpectKeyword(Keyword.For, "Expected 'For'.");

            Token id = Expect(TokenKind.Identifier, "Expected loop variable.");
            ExpectKeyword(Keyword.In, "Expected 'In' after loop variable.");

            Expr iterable = Expression();

            _loops.Enter();
            StmtBlock body = new(ParseStatements(Keyword.EndFor));
            _loops.Exit();

            ExpectKeyword(Keyword.EndFor, "Expected 'EndFor'.");

            return new StmtFor(id, iterable, body);
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

            while (MatchOp(OperatorKind.Or) || MatchKeyword(Keyword.Or))
                expr = new ExprBinary(expr, Previous, And());

            return expr;
        }

        Expr And()
        {
            Expr expr = Equality();

            while (MatchOp(OperatorKind.And) || MatchKeyword(Keyword.And))
                expr = new ExprBinary(expr, Previous, Equality());

            return expr;
        }

        Expr Equality()
        {
            Expr expr = Comparison();

            while (MatchOp(OperatorKind.Equal, OperatorKind.NotEqual) || MatchKeyword(Keyword.Is))
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

            while (MatchOp(OperatorKind.Multiply, OperatorKind.Divide, OperatorKind.Modulo))
                expr = new ExprBinary(expr, Previous, Power());

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
            if (MatchOp(OperatorKind.Plus, OperatorKind.Minus, OperatorKind.Not) ||
                MatchKeyword(Keyword.Not))
            {
                return new ExprUnary(Previous, Unary());
            }

            return Postfix();
        }

        Expr Postfix()
        {
            Expr expr = Primary();

            while (true)
            {
                if (Match(TokenKind.OpenParen))
                {
                    Token open = Previous;
                    List<Expr> args = [];

                    SkipSeparators();

                    if (!Check(TokenKind.CloseParen))
                    {
                        do
                        {
                            SkipSeparators();
                            args.Add(Expression());
                            SkipSeparators();
                        }
                        while (Match(TokenKind.Comma));
                    }

                    SkipSeparators();

                    Token close = Expect(TokenKind.CloseParen, "Expected ')' after call arguments.");
                    expr = new ExprCall(expr, open, args, close);
                    continue;
                }

                if (Match(TokenKind.OpenSquare))
                {
                    Token open = Previous;
                    Expr? index = null;

                    SkipSeparators();

                    if (!Check(TokenKind.CloseSquare))
                        index = Expression();

                    SkipSeparators();

                    Token close = Expect(TokenKind.CloseSquare, "Expected ']'.");
                    expr = new ExprIndex(expr, open, index, close);
                    continue;
                }

                if (Match(TokenKind.Dot))
                {
                    Token dot = Previous;
                    Token member = ExpectIdentifierLike("Expected member name after '.'.");
                    expr = new ExprMember(expr, dot, member);
                    continue;
                }

                break;
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
                return new ExprString(Previous, Previous.Text);

            if (Match(TokenKind.Identifier))
                return new ExprIdentifier(Previous);

            if (MatchKeyword(Keyword.True))
                return new ExprBoolean(Previous, true);

            if (MatchKeyword(Keyword.False))
                return new ExprBoolean(Previous, false);

            if (Match(TokenKind.OpenParen))
            {
                Token open = Previous;
                Expr inner = Expression();
                Token close = Expect(TokenKind.CloseParen, "Expected ')'.");
                return new ExprGroup(open, inner, close);
            }

            if (Check(TokenKind.OpenCurly))
                return ListExpression();

            return ExprErrorAt(Peek(), $"Expected expression but got '{Peek().Kind}'.");
        }

        void SkipSeparators()
        {
            while (Match(TokenKind.LineBreak))
            {
            }
        }

        Expr ExpressionSkipSeparators()
        {
            SkipSeparators();

            Expr expr = Expression();

            SkipSeparators();

            return expr;
        }

        Expr ListExpression()
        {
            Token open = Expect(TokenKind.OpenCurly, "Expected '{'.");

            SkipSeparators();

            if (Match(TokenKind.CloseCurly))
                return new ExprList(open, [], Previous);

            Expr first = Expression();

            SkipSeparators();

            if (Match(TokenKind.Colon))
            {
                Token colon1 = Previous;

                Expr end = ExpressionSkipSeparators();

                Token? colon2 = null;
                Expr? step = null;

                if (Match(TokenKind.Colon))
                {
                    colon2 = Previous;

                    step = ExpressionSkipSeparators();
                }

                Expect(TokenKind.CloseCurly, "Expected '}' after range.");
                return new ExprRange(first, colon1, end, colon2, step);
            }

            List<Expr> values = [first];

            while (true)
            {
                SkipSeparators();

                if (!Match(TokenKind.Comma))
                    break;

                SkipSeparators();

                if (Check(TokenKind.CloseCurly))
                    break;

                values.Add(Expression());
            }

            SkipSeparators();

            Token close = Expect(TokenKind.CloseCurly, "Expected '}' after list.");
            return new ExprList(open, values, close);
        }

        Expr ParenthesizedExpression()
        {
            Expect(TokenKind.OpenParen, "Expected '('.");
            Expr expr = Expression();
            Expect(TokenKind.CloseParen, "Expected ')'.");
            return expr;
        }

        ExprNumber Number(Token token)
        {
            string text = token.Text;

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return new ExprNumber(token, i);

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return new ExprNumber(token, d);

            return new ExprNumber(token, double.NaN);
        }

        Keyword EntityKindKeyword()
        {
            if (MatchKeyword(Keyword.Point)) return Keyword.Point;
            if (MatchKeyword(Keyword.Line)) return Keyword.Curve;
            if (MatchKeyword(Keyword.Curve)) return Keyword.Curve;
            if (MatchKeyword(Keyword.Surface)) return Keyword.Surface;

            StmtErrorAt(Peek(), "Expected entity kind Point, Line, Curve, or Surface.");
            return Keyword.None;
        }

        bool IsEmbedStart()
        {
            return Peek().Is(Keyword.Line) && Peek(1).Kind == TokenKind.OpenCurly ||
                   Peek().Is(Keyword.Curve) && Peek(1).Kind == TokenKind.OpenCurly ||
                   Peek().Is(Keyword.Point) && Peek(1).Kind == TokenKind.OpenCurly ||
                   Peek().Is(Keyword.Surface) && Peek(1).Kind == TokenKind.OpenCurly;
        }

        bool IsCommandStart()
        {
            return Peek().Is(Keyword.Coherence) ||
                   Peek().Is(Keyword.RenumberMeshNodes) ||
                   Peek().Is(Keyword.RenumberMeshElements) ||
                   Peek().Is(Keyword.Refine) ||
                   Peek().Is(Keyword.Optimize);
        }

        List<Token> ReadPath()
        {
            List<Token> path = [ExpectIdentifierLike("Expected path segment.")];

            while (Match(TokenKind.Dot))
                path.Add(ExpectIdentifierLike("Expected path segment after '.'."));

            return path;
        }

        List<Token> ReadCommandPath()
        {
            List<Token> path = [ExpectIdentifierLike("Expected command name.")];

            while (Peek().Kind is TokenKind.Identifier or TokenKind.Keyword &&
                   !Peek().Is(Keyword.If) &&
                   !Peek().Is(Keyword.For) &&
                   !Peek().Is(Keyword.While))
            {
                path.Add(Advance());
            }

            return path;
        }

        Token ExpectIdentifierLike(string message)
        {
            if (Peek().Kind is TokenKind.Identifier or TokenKind.Keyword)
                return Advance();

            Report(Peek(), message);
            Synchronize();
            return Peek();
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
            return token.ScanError switch
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

        void Synchronize()
        {
            if (IsAtEnd())
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
            return token.Kind is TokenKind.Semicolon or TokenKind.LineBreak or TokenKind.EndOfFile;
        }

        static bool IsStatementStarter(Token token)
        {
            if (token.Kind != TokenKind.Keyword)
                return false;

            return token.Keyword is
                Keyword.If or Keyword.For or Keyword.While or
                Keyword.Break or Keyword.Continue or
                Keyword.Point or Keyword.Line or Keyword.Circle or
                Keyword.Ellipse or Keyword.Spline or Keyword.BSpline or
                Keyword.Bezier or Keyword.Curve or Keyword.Plane or
                Keyword.Physical or Keyword.Transfinite or Keyword.Recombine or
                Keyword.Mesh or Keyword.Coherence or
                Keyword.RenumberMeshNodes or Keyword.RenumberMeshElements or
                Keyword.ElseIf or Keyword.Else or Keyword.EndIf or
                Keyword.EndFor or Keyword.EndWhile;
        }

        void SkipLineBreaks()
        {
            while (Match(TokenKind.LineBreak)) { }
        }

        Token Peek(int offset = 0) => _scanner.Peek(offset);
        Token Advance() => _scanner.Consume();
        Token Previous => _scanner.Previous;

        bool IsAtEnd() => Peek().Kind == TokenKind.EndOfFile;
        bool Check(TokenKind kind) => Peek().Kind == kind;

        bool CheckKeyword(Keyword keyword)
            => Peek().Is(keyword);

        bool Match(params TokenKind[] kinds)
        {
            TokenKind current = Peek().Kind;

            foreach (TokenKind kind in kinds)
            {
                if (current == kind)
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        bool MatchKeyword(Keyword keyword)
        {
            if (!Peek().Is(keyword))
                return false;

            Advance();
            return true;
        }

        bool MatchOp(params OperatorKind[] ops)
        {
            if (Peek().Kind != TokenKind.Operator)
                return false;

            OperatorKind current = Peek().Operator;

            foreach (OperatorKind op in ops)
            {
                if (current == op)
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        bool CheckKeywordAny(params Keyword[] keywords)
        {
            if (Peek().Kind != TokenKind.Keyword)
                return false;

            foreach (Keyword keyword in keywords)
                if (Peek().Keyword == keyword)
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

        Token ExpectOp(OperatorKind op, string message)
        {
            if (MatchOp(op))
                return Previous;

            Report(Peek(), message);
            Synchronize();
            return Peek();
        }
    }
}
