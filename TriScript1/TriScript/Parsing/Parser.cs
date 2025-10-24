using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;
using static System.Net.Mime.MediaTypeNames;

namespace TriScript.Parsing
{
    public class Parser
    {
        readonly Scanner _scanner;

        public Parser(Source source, ScopeStack scope, Diagnostics diagnostics)
        {
            _scanner = new Scanner(source);
            Scope = scope;
            Diagnostics = diagnostics;
        }

        public Source Source => _scanner.Source;
        public ScopeStack Scope { get; }
        public Diagnostics Diagnostics { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Token Peek(int offset = 0) => _scanner.Peek(offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Token Advance() => _scanner.Consume();

        Token Previous => _scanner.Previous;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsAtEnd() => Peek().type == ETokenType.EndOfFile;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Check(ETokenType t) => Peek().type == t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Match(params ETokenType[] types)
        {
            var t = Peek().type;
            for (int i = 0; i < types.Length; i++)
                if (t == types[i]) { Advance(); return true; }
            return false;
        }

        Token Expect(ETokenType type, string message)
        {
            var token = Peek();
            if (token.type == type) return Advance();

            Diagnostics.Report(Source, ESeverity.Error, message, token);
            Synchronize();
            return token; // return what we saw so callers can continue
        }

        void SkipTrivia()
        {
            while (Check(ETokenType.LineBreak))
                Advance();
        }

        public StmtProgram Parse()
        {
            var first = Peek();
            return new StmtProgram(first, new StmtBlock(first, ParseStatements()));
        }

        List<Stmt> ParseStatements()
        {
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                SkipTrivia();

                if (IsAtEnd() || Check(ETokenType.CloseCurly))
                    break;

                var token = Peek();
                switch (token.type)
                {
                    case ETokenType.Print:
                        statements.Add(Print());
                        break;

                    default:
                        var expr = Expression();
                        statements.Add(new StmtExpr(token, expr));
                        break;
                }

                if (Match(ETokenType.SemiColon)) { /* consume ; */ }
                SkipTrivia();
            }
            return statements;
        }

        StmtPrint Print()
        {
            var tkn = Expect(ETokenType.Print, $"Expected '{ETokenType.Print}'.");
            var args = Arguments();
            return new StmtPrint(tkn, args);
        }

        Expr Expression() => Or();

        Expr Or()
        {
            var expr = And();
            while (Match(ETokenType.Or))
                expr = new ExprBinary(expr, Previous, And());
            return expr;
        }

        Expr And()
        {
            var expr = Equality();
            while (Match(ETokenType.And))
                expr = new ExprBinary(expr, Previous, Equality());
            return expr;
        }

        Expr Equality()
        {
            var expr = Comparison();
            while (Match(ETokenType.Equal, ETokenType.NotEqual))
                expr = new ExprBinary(expr, Previous, Comparison());
            return expr;
        }

        Expr Comparison()
        {
            var expr = Term();
            while (Match(ETokenType.Greater, ETokenType.GreaterEqual, ETokenType.Less, ETokenType.LessEqual))
                expr = new ExprBinary(expr, Previous, Term());
            return expr;
        }

        Expr Term()
        {
            var expr = Factor();
            while (Match(ETokenType.Minus, ETokenType.Plus))
                expr = new ExprBinary(expr, Previous, Factor());
            return expr;
        }

        Expr Factor()
        {
            var expr = UnaryPrefix();
            while (Match(ETokenType.Star, ETokenType.Slash))
                expr = new ExprBinary(expr, Previous, UnaryPrefix());
            return expr;
        }

        Expr UnaryPrefix()
        {
            if (Match(ETokenType.Minus, ETokenType.Plus, ETokenType.Not, ETokenType.MinusMinus, ETokenType.PlusPlus))
                return new ExprUnaryPrefix(Previous, UnaryPrefix());
            return UnaryPostfix();
        }

        Expr UnaryPostfix()
        {
            var expr = Primary();
            while (Match(ETokenType.PlusPlus, ETokenType.MinusMinus))
                expr = new ExprUnaryPostfix(expr, Previous);
            return expr;
        }

        Expr Primary()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.LiteralSymbol:
                    {
                        Token symTok = Advance();
                        string symbolName = Source.GetString(symTok.span);
                        ExprLiteralSymbol symExpr = new ExprLiteralSymbol(symTok, new Value(new ObjSymbol(symbolName)));

                        // assignment (right-associative): id '=' expression
                        if (Match(ETokenType.Assign))
                            return new ExprAssignment(symExpr, Previous, Expression());

                        return symExpr;
                    }

                case ETokenType.LiteralString:
                    {
                        var strTok = Advance();
                        var text = Source.GetString(strTok.span);
                        return new ExprLiteralSymbol(strTok, new Value(new ObjString(text)));
                    }

                case ETokenType.LiteralNumeric:
                    {
                        var numTok = Advance();
                        var txt = Source.GetString(numTok.span);

                        if (int.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                            return new ExprLiteralInteger(numTok, new Value(i));

                        if (double.TryParse(txt, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                            return new ExprLiteralReal(numTok, new Value(d));

                        Diagnostics.Report(Source, ESeverity.Error, $"Failed to parse number '{txt}'.", numTok);
                        return new ExprError(numTok);
                    }

                case ETokenType.OpenParen:
                    {
                        var open = Advance();
                        var inner = Expression();
                        Expect(ETokenType.CloseParen, "Expected closing ')'.");
                        return new ExprGroup(open, inner, Previous);
                    }
            }

            // Unexpected primary
            var bad = Advance();
            Diagnostics.Report(Source, ESeverity.Error, "Unexpected token.", bad);
            Synchronize();
            return new ExprError(bad);
        }

        List<Expr> Arguments(
            ETokenType open = ETokenType.OpenParen,
            ETokenType close = ETokenType.CloseParen,
            ETokenType separator = ETokenType.Comma)
        {
            Expect(open, $"Expected '{open}'.");
            var args = new List<Expr>(4);

            bool sawSeparator = false;

            if (!Check(close))
            {
                while (true)
                {
                    args.Add(Expression());

                    if (!Match(separator))
                        break;

                    sawSeparator = true;

                    // Trailing comma allowed: (..., )
                    if (Check(close))
                        break;
                }
            }

            var end = Expect(close, $"Expected '{close}'.");

            // Edge case: "(,)" — empty list but had a separator
            if (args.Count == 0 && sawSeparator)
                Diagnostics.Report(Source, ESeverity.Error, "Unexpected trailing comma in empty argument list.", end);

            return args;
        }

        void Synchronize()
        {
            if (!IsAtEnd()) Advance();

            while (!IsAtEnd())
            {
                switch (Peek().type)
                {
                    case ETokenType.SemiColon:
                    case ETokenType.LineBreak:
                    case ETokenType.CloseCurly:
                    case ETokenType.CloseParen:
                    case ETokenType.EndOfFile:
                        return;
                }
                Advance();
            }
        }
    }
}
