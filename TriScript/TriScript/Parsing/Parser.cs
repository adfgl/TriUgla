using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TriScript.Data;
using TriScript.Data.Units;
using TriScript.Diagnostics;
using TriScript.Parsing.Nodes;
using TriScript.Parsing.Nodes.Expressions;
using TriScript.Parsing.Nodes.Expressions.Literals;
using TriScript.Parsing.Nodes.Statements;
using TriScript.Parsing.Nodes.Units;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public class Parser
    {
        readonly Source _source;
        readonly Scanner _scanner;
        readonly DiagnosticBag _diagnostics;
        readonly ScopeStack _stack = new ScopeStack();
        readonly UnitRegistry _unitReg = new UnitRegistry();


        public Parser(Source source, DiagnosticBag diagnostic)
        {
            _source = source;
            _scanner = new Scanner(_source);
            _diagnostics = diagnostic;
        }

        public DiagnosticBag Diagnostics => _diagnostics;
        public Source Source => _scanner.Source;

        public StmtProgram Parse()
        {
            _stack.OpenScope();
            StmtProgram prog =  new StmtProgram(new StmtBlock(ParseStatements()));
            _stack.CloseScope();
            return prog;
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

                    case ETokenType.CloseCurly:
                        return statements;

                    case ETokenType.Print:
                        statements.Add(Print());
                        break;

                    default:
                        statements.Add(new StmtExpr(Expression()));
                        break;
                }
            }
            return statements;
        }

        StmtPrint Print()
        {
            Consume(ETokenType.Print);
            List<Expr> args = Arguments();
            return new StmtPrint(args);
        }


        #region EXPRESSION 
        Expr Expression()
        {
            Expr expr = Or();

            while (Peek().type == ETokenType.OpenSquare)
            {
                Consume(ETokenType.OpenSquare);
                var uAst = ParseUExpr();
                Consume(ETokenType.CloseSquare);

                UnitEval uEval = uAst.Evaluate(_unitReg, _diagnostics);
                expr = new ExprWithUnit(expr, uEval);
            }

            return expr;
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

        Expr Equality()
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

        Expr Primary()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.LiteralIdentifier:
                    if (Peek(1).type == ETokenType.OpenParen)
                    {
                        return Call();
                    }
                    return AssignmentOrDeclaration();

                case ETokenType.LiteralString:
                    return new ExprString(Consume());

                case ETokenType.LiteralNemeric:
                    return Number();

                case ETokenType.OpenParen:
                    return Group();
            }

            Consume();
            _diagnostics.Report(ESeverity.Error, $"Unexpected token.", token.span);
            Synchronize();
            return new ExprError(token);
        }
        #endregion EXPRESSION

        #region UNIT EXPRESSION
        UnitExpr ParseUExpr()
        {
            UnitExpr left = ParseUTerm();
            while (true)
            {
                var t = Peek();
                if (t.type == ETokenType.Star || t.type == ETokenType.Slash)
                {
                    var op = Consume();
                    var right = ParseUTerm();
                    left = new UnitBinary(left, op, right);
                    continue;
                }
                if (t.type == ETokenType.Plus || t.type == ETokenType.Minus)
                {
                    var bad = Consume();
                    _diagnostics.Report(ESeverity.Error, "Units cannot use '+' or '-' (except as exponent sign).", bad.span);
                    continue;
                }
                break;
            }
            return left;
        }

        UnitExpr ParseUTerm()
        {
            UnitExpr node = ParseUPrimary();

            if (Peek().type == ETokenType.Pow)
            {
                var caret = Consume(ETokenType.Pow);
                int exp = ParseUnitSignedInt();
                node = new UnitBinary(node, caret, new UnitInt(_scanner.Previous, exp));
            }
            return node;
        }

        UnitExpr ParseUPrimary()
        {
            var t = Peek();

            if (t.type == ETokenType.OpenParen)
            {
                var open = Consume(ETokenType.OpenParen);
                var inner = ParseUExpr();
                var close = Consume(ETokenType.CloseParen);
                return new UnitGroup(open, inner, close);
            }

            if (t.type == ETokenType.LiteralIdentifier)
            {
                var id = Consume(ETokenType.LiteralIdentifier);
                string name = Source.GetString(id.span);
                return new UnitSymbol(id, name);
            }

            // allow '1' for unity, so [1/m] works
            if (t.type == ETokenType.LiteralNemeric)
            {
                var num = Consume(ETokenType.LiteralNemeric);
                string s = Source.GetString(num.span);
                if (s == "1") return new UnitSymbol(num, "1");

                _diagnostics.Report(ESeverity.Error, "Only '1' is allowed as a number in unit expressions.", num.span);
                return new UnitSymbol(num, "1"); // recover
            }

            Consume();
            _diagnostics.Report(ESeverity.Error, "Expected unit symbol, '1', or '(' in unit expression.", t.span);
            return new UnitSymbol(t, "1");
        }

        int ParseUnitSignedInt()
        {
            int sign = 1;
            if (Peek().type == ETokenType.Plus) Consume();
            else if (Peek().type == ETokenType.Minus) { Consume(); sign = -1; }

            var tok = Consume(ETokenType.LiteralNemeric);
            string s = Source.GetString(tok.span);
            if (!int.TryParse(s, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int val))
            {
                _diagnostics.Report(ESeverity.Error, "Expected integer exponent after '^'.", tok.span);
                return 1;
            }
            return sign * val;
        }
        #endregion UNIT EXPRESSION

        Expr AssignmentOrDeclaration()
        {
            Token tknId = Consume(ETokenType.LiteralIdentifier);
            string name = Source.GetString(tknId.span);

            bool fetched = _stack.Current.TryGet(name, out Variable var);

            if (Peek().type == ETokenType.Assign)
            {
                Consume(); // '='

                Expr value = Expression();

                if (!fetched)
                {
                    var = new Variable(name);
                    _stack.Current.Declare(var);
                }

                // Preview the RHS type ONCE
                EDataType rhsType = value.PreviewType(_source, _stack, _diagnostics);

                if (rhsType == EDataType.None)
                {
                    _diagnostics.Report(
                        ESeverity.Error,
                        $"Cannot infer the type of the right-hand side for '{name}'.",
                        value.Token.span);
                }
                else
                {
                    EDataType current = var.Value.type;

                    if (current == EDataType.None)
                    {
                        // First assignment declares the type
                        var.Value = new Value(rhsType);
                    }
                    else if (!CanImplicitlyConvert(rhsType, current))
                    {
                        _diagnostics.Report(
                            ESeverity.Error,
                            $"Cannot assign '{rhsType}' to variable '{name}' of type '{current}'.",
                            value.Token.span);
                    }
                }
                return new ExprAssignment(tknId, value);
            }

            if (!fetched)
            {
                _diagnostics.Report(ESeverity.Error, $"Use of unassigned variable '{name}'.", tknId.span);
            }

            return new ExprIdentifier(tknId);
        }

        public static bool CanImplicitlyConvert(EDataType from, EDataType to)
            => from == to || (from == EDataType.Integer && to == EDataType.Real);

        Expr Number()
        {
            Token numeric = Consume(ETokenType.LiteralNemeric);
            string lexeme = Source.GetString(numeric.span);
            if (int.TryParse(lexeme, out int integer))
            {
                return new ExprNumeric(numeric, new Value(integer));
            }
            else if (double.TryParse(lexeme, out double real))
            {
                return new ExprNumeric(numeric, new Value(real));
            }

            _diagnostics.Report(ESeverity.Error, $"Unable to parse numeric.", numeric.span);
            return new ExprNumeric(numeric, new Value(Double.NaN));
        }

        ExprCall Call()
        {
            Token collee = Consume(ETokenType.LiteralIdentifier);
            List<Expr> args = Arguments(ETokenType.OpenParen, ETokenType.CloseParen, ETokenType.Comma);
            return new ExprCall(collee, args);  
        }

        ExprGroup Group()
        {
            Token open = Consume(ETokenType.OpenParen);
            Expr group = Expression();
            Token close = Consume(ETokenType.CloseParen);
            return new ExprGroup(open, group, close);
        }

        List<Expr> Arguments(
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
                    case ETokenType.Else:
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
