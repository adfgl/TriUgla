using System;
using System.Runtime.CompilerServices;
using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Parsing.Nodes;
using TriScript.Parsing.Nodes.Expressions;
using TriScript.Parsing.Nodes.Expressions.Literals;
using TriScript.Parsing.Nodes.Statements;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public class Parser
    {
        readonly Scanner _scanner;
        readonly DiagnosticBag _diagnostics;
        readonly ScopeStack _scope = new ScopeStack();

        public Parser(Source source, DiagnosticBag diagnostic)
        {
            _scanner = new Scanner(source);
            _diagnostics = diagnostic;
        }

        public DiagnosticBag Diagnostics => _diagnostics;
        public Source Source => _scanner.Source;

        public TriProgram Parse()
        {
            return new TriProgram(Block());
        }

        Token Consume() => _scanner.Consume(_diagnostics);

        public Token Consume(ETokenType type)
        {
            Token token = Peek();
            if (token.type != type)
            {
                _diagnostics.Report(ESeverity.Error, $"Syntax error: expected '{type}' but got '{token.type}'", token.span);
                Synchronize();
                return token;
            }
            return Consume();
        }

        bool IsAtEnd() => Peek().type == ETokenType.EndOfFile;

        Token Peek(int offset = 0)
        {
            return _scanner.Peek(_diagnostics, offset);
        }

        bool Match(params ETokenType[] types)
        {
            Token token = Peek();
            foreach (ETokenType type in types)
            {
                if (token.type == type)
                {
                    Consume();
                    return true;
                }
            }
            return false;
        }

        List<Stmt> ParseStatements()
        {
            List<Stmt> statements = new List<Stmt>();

            Token token;
            while ((token = Peek()).type != ETokenType.EndOfFile)
            {
                switch (token.type)
                {
                    case ETokenType.LineBreak:
                        Consume();
                        break;

                    default:
                        statements.Add(Statement());
                        break;
                }
            }
            return statements;
        }

        Stmt Statement()
        {
            return new StmtExpr(Expression());
        }

        StmtBlock Block()
        {
            _scope.OpenScope();
            List<Stmt> stmts = ParseStatements();
            _scope.CloseScope(); 
            return new StmtBlock(stmts);
        }

        Expr Expression()
        {
            return Or();
        }

        Expr Or()
        {
            Expr expr = And();
            while (Match(ETokenType.Or))
            {
                Token op = _scanner.Previous;
                Expr right = And();
                expr = new ExprBinary(expr, op, right);
            }
            return expr;
        }

        Expr And()
        {
            Expr expr = Equality();
            while (Match(ETokenType.And))
            {
                Token op = _scanner.Previous;
                Expr right = Equality();
                expr = new ExprBinary(expr, op, right);
            }
            return expr;
        }

        Expr Equality() // c9
        {
            Expr expr = Comparison();
            while (Match(ETokenType.Equal, ETokenType.NotEqual))
            {
                Token op = _scanner.Previous;
                Expr right = Comparison();
                expr = new ExprBinary(expr, op, right);
            }
            return expr;
        }

        Expr Comparison()
        {
            Expr expr = Term();
            while (Match(
                ETokenType.Greater, ETokenType.GreaterEqual,
                ETokenType.Less, ETokenType.LessEqaul))

            {
                Token op = _scanner.Previous;
                Expr right = Term();
                expr = new ExprBinary(expr, op, right);
            }
            return expr;
        }

        Expr Term()
        {
            Expr expr = Factor();
            while (Match(ETokenType.Minus, ETokenType.Plus))
            {
                Token op = _scanner.Previous;
                Expr right = Factor();
                expr = new ExprBinary(expr, op, right);
            }
            return expr;
        }

        Expr Factor()
        {
            Expr expr = UnaryPrefix();
            while (Match(ETokenType.Star, ETokenType.Slash))
            {
                Token op = _scanner.Previous;
                Expr right = UnaryPrefix();
                expr = new ExprBinary(expr, op, right);
            }
            return expr;
        }

        Expr UnaryPrefix()
        {
            if (Match(ETokenType.Minus, ETokenType.Plus, ETokenType.Not, ETokenType.MinusMinus, ETokenType.PlusPlus))
            {
                Token op = _scanner.Previous;
                Expr right = UnaryPrefix();
                return new ExprUnaryPrefix(op, right);
            }
            return UnaryPostfix();
        }

        Expr UnaryPostfix()
        {
            if (Match(ETokenType.MinusMinus, ETokenType.PlusPlus))
            {
                Token op = _scanner.Previous;
                Expr right = UnaryPrefix();
                return new ExprUnaryPostfix(right, op);
            }
            return Primary();
        }

        Expr Assignment()
        {
            Token id = Consume(ETokenType.LiteralIdentifier);
            string name = Source.GetString(id.span);

            List<Expr> args = [];
            if (Peek().type == ETokenType.OpenSquare)
            {
                args = ParseArguments(ETokenType.OpenSquare, ETokenType.CloseSquare);
            }

            bool fetched = _scope.CurrentScope.TryGet(name, out Variable var);

            if (Peek().type == ETokenType.Assign)
            {
                Consume();

                Expr value = Expression();

                if (!fetched)
                {
                    var = new Variable(name);
                    _scope.CurrentScope.Declare(var);

                    if (args.Count == 0)
                    {
                        return new ExprAssignment(id, value);
                    }
                    return new ExprAssignmentByIndex(id, args, value);
                }
            }

            if (!fetched)
            {
                _diagnostics.Report(ESeverity.Error, $"Use of unassigned variable '{name}'.", id.span);
            }
            return new ExprIdentifier(id);
        }

        Expr Number()
        {
            Token numeric = Consume(ETokenType.LiteralNemeric);
            string lexeme = Source.GetString(numeric.span);
            if (int.TryParse(lexeme, out _))
            {
                return new ExprInteger(numeric);
            }
            else if (double.TryParse(lexeme, out _))
            {
                return new ExprReal(numeric);
            }

            _diagnostics.Report(ESeverity.Error, $"Unable to parse numeric.", numeric.span);
            return new ExprReal(numeric);
        }

        Expr Primary()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.LiteralIdentifier:
                    return Assignment();

                case ETokenType.True:
                case ETokenType.False:
                    return new ExprBoolean(Consume());

                case ETokenType.LiteralSymbol:
                    return new ExprCharacter(Consume());

                case ETokenType.LiteralString:
                    return new ExprString(Consume());

                case ETokenType.LiteralNemeric:
                    return Number();

                case ETokenType.OpenCurly:
                    return Matrix();

                case ETokenType.OpenParen:
                    if (_scanner.Previous.type == ETokenType.LiteralIdentifier)
                    {
                        return Call();
                    }
                    return Group();
            }

            Consume();
            _diagnostics.Report(ESeverity.Error, $"Unexpected token.", token.span);
            Synchronize();
            return new ExprError();
        }

        ExprMatrix Matrix()
        {
            Token open = Consume(ETokenType.OpenCurly);
            SkipTrivia();

            List<List<Expr>> rows = new List<List<Expr>>();

            if (Peek().type == ETokenType.CloseCurly)
            {
                _diagnostics.Report(ESeverity.Error, "Empty matrix is not allowed.", Peek().span);
                Consume(ETokenType.CloseCurly);
                return new ExprMatrix(open, null, null, []);
            }

            while (true)
            {
                List<Expr> row = MatrixRow();
                if (row.Count == 0)
                {
                    _diagnostics.Report(ESeverity.Error, "Matrix row cannot be empty.", _scanner.Previous.span);
                }
                rows.Add(row);

                SkipTrivia();

                if (Match(ETokenType.SemiColon))
                {
                    SkipTrivia();
                    if (Peek().type == ETokenType.CloseCurly)
                    {
                        // trailing ';' before '}' allowed
                        break;
                    }
                    // else: another row expected, continue loop
                    continue;
                }
                // No ';' → must be closing '}' (end of matrix)
                break;
            }

            Consume(ETokenType.CloseCurly);

            int cols = rows[0].Count;
            for (int r = 1; r < rows.Count; r++)
            {
                if (rows[r].Count != cols)
                {
                    _diagnostics.Report(
                        ESeverity.Error,
                        $"All matrix rows must have the same number of elements. Row 0 has {cols}, row {r} has {rows[r].Count}.",
                        _scanner.Previous.span);
                    break; // avoid cascades
                }
            }

            List<Expr> args = new List<Expr>(rows.Count * rows[0].Count);
            foreach (List<Expr> row in rows)
            {
                foreach (Expr value in row)
                {
                    args.Add(value);

                    if (value is ExprIdentifier id)
                    {
                        string idStr = Source.GetString(id.Token.span);
                        if (!_scope.CurrentScope.TryGet(idStr, out _))
                        {
                            // not declared
                        }
                    }
                    else if (value is ExprInteger or ExprReal)
                    {
                        args.Add(value);
                    }
                    else
                    {
                        // not allowed
                    }
                }
            }
            return null;
        }

        List<Expr> MatrixRow()
        {
            var elems = new List<Expr>(8);

            while (true)
            {
                SkipTrivia();

                // Stop row at row/Matrix terminators (don't require a trailing separator)
                var t = Peek().type;
                if (t == ETokenType.SemiColon || t == ETokenType.CloseCurly)
                    break;

                // Parse an element expression
                elems.Add(Expression());
                SkipTrivia();

                // Optional comma between elements
                if (Match(ETokenType.Comma))
                {
                    // allow comma + whitespace; next loop iteration decides if row ends
                    continue;
                }

                // If next is a terminator we’ll break at the top of the loop.
                // Otherwise, whitespace-separated elements are allowed → continue.
            }
            return elems;
        }

        ExprCall Call()
        {
            Token collee = Consume(ETokenType.LiteralIdentifier);
            List<Expr> args = ParseArguments(ETokenType.OpenParen, ETokenType.CloseParen, ETokenType.Comma);

            return new ExprCall(collee, args);  
        }

        ExprGroup Group()
        {
            Token open = Consume(ETokenType.OpenParen);
            Expr group = Expression();
            Token close = Consume(ETokenType.CloseParen);
            return new ExprGroup(open, group, close);
        }

        List<Expr> ParseArguments(
            ETokenType open = ETokenType.OpenParen,
            ETokenType close = ETokenType.CloseParen,
            ETokenType separator = ETokenType.Comma)
        {
            Consume(open);
            List<Expr> args = new List<Expr>(4);

            bool allowTrailing = false;

            if (Peek().type != close)
            {
                while (true)
                {
                    args.Add(Expression());

                    if (!Match(separator))
                        break;

                    // if next is close, this is the trailing comma — only valid if at least one arg
                    if (Peek().type == close)
                    {
                        allowTrailing = args.Count > 0;
                        break;
                    }
                }
            }

            // consume closing parenthesis
            Token end = Consume(close);

            // invalid case: trailing comma but no args
            if (!allowTrailing && args.Count == 0 && _scanner.Previous.type == separator)
            {
                _diagnostics.Report(ESeverity.Error, "Unexpected trailing comma in empty argument list.", end.span);
            }
            return args;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SkipTrivia()
        {
            while (true)
            {
                var t = Peek();
                if (t.type == ETokenType.LineBreak) { Consume(); continue; }
                break;
            }
        }

        void Synchronize()
        {
            // Advance once so we don't loop forever on the offending token.
            Consume();

            while (!IsAtEnd())
            {
                // If we *just* consumed a terminator, we're at a fresh boundary.
                var prev = _scanner.Previous.type;
                if (prev == ETokenType.SemiColon ||
                    prev == ETokenType.LineBreak ||
                    prev == ETokenType.CloseCurly ||
                    prev == ETokenType.CloseParen)
                    return;

                // If the *next* token looks like a statement start or block delimiter, stop.
                switch (Peek().type)
                {
                    case ETokenType.If:
                    case ETokenType.Elif:
                    case ETokenType.Else:
                    case ETokenType.Then:
                    case ETokenType.For:
                    case ETokenType.Break:
                    case ETokenType.Continue:
                    case ETokenType.OpenCurly:
                    case ETokenType.CloseCurly:
                    case ETokenType.EndOfFile:
                        return;
                }

                // Otherwise, keep dumping tokens.
                Consume();
            }
        }
    }
}
