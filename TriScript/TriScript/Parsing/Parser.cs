using System;
using System.Runtime.CompilerServices;
using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Parsing.Nodes;
using TriScript.Parsing.Nodes.Expressions;
using TriScript.Parsing.Nodes.Expressions.Literals;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public class Parser
    {
        int _loopDepth;
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

        //public NodeExprProgram Parse()
        //{

        //}


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

        Expr Primary()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.True:
                    return new ExprBoolean(Consume());

                case ETokenType.LiteralSymbol:
                    return new ExprCharacter(Consume());

                case ETokenType.LiteralString:
                    return new ExprString(Consume());

                case ETokenType.LiteralNemeric:
                    string lexeme = Source.GetString(token.span);
                    if (int.TryParse(lexeme, out _))
                    {
                        return new ExprInteger(Consume());
                    }
                    else if (double.TryParse(lexeme, out _))
                    {
                        return new ExprReal(Consume());
                    }
                    break;

                case ETokenType.OpenCurly:
               
                    break;

                case ETokenType.OpenParen:
                    Consume();
                    Expr group = Expression();
                    Consume(ETokenType.CloseParen);
                    return group;
            }

            Consume();
            _diagnostics.Report(ESeverity.Error, $"Unexpected token.", token.span);
            Synchronize();
            return new ExprError();
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
