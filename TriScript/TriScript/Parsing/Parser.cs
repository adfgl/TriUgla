using System;
using System.Runtime.CompilerServices;
using TriScript.Data;
using TriScript.Diagnostics;
using TriScript.Parsing.Nodes;
using TriScript.Parsing.Nodes.Expressions;
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

        Token _previous = new Token();

        Token Consume()
        {
            _previous = _scanner.Consume(_diagnostics);
            return _previous;
        }

        public Token Consume(ETokenType type)
        {
            Token token = Peek();
            if (token.type != type)
            {
                _diagnostics.Report(ESeverity.Error, $"Syntax error: expected '{type}' but got '{token.type}'", token.span);
            }
            return Consume();
        }

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

        bool TryConsume(ETokenType type, out Token token)
        {
            token = Peek();
            if (token.type == type)
            {
                token = Consume();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        static bool IsEOX(ETokenType type)
        {
            return type == ETokenType.EndOfFile || type == ETokenType.LineBreak;
        }

        void MaybeEOX()
        {
            if (IsEOX(Peek().type)) Consume();
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
                Token op = _previous;
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
                Token op = _previous;
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
                Token op = _previous;
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
                Token op = _previous;
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
                Token op = _previous;
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
                Token op = _previous;
                Expr right = UnaryPrefix();
                expr = new ExprBinary(expr, op, right);
            }
            return expr;
        }

        Expr UnaryPrefix()
        {
            if (Match(ETokenType.Minus, ETokenType.Plus, ETokenType.Not, ETokenType.MinusMinus, ETokenType.PlusPlus))
            {
                Token op = _previous;
                Expr right = UnaryPrefix();
                return new ExprUnaryPrefix(op, right);
            }
            return UnaryPostfix();
        }

        Expr UnaryPostfix()
        {
            if (Match(ETokenType.MinusMinus, ETokenType.PlusPlus))
            {
                Token op = _previous;
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

                case ETokenType.LiteralNemeric:
                    return ParseNumber();

                case ETokenType.LiteralString:
                    return new ExprString(Consume());

                case ETokenType.OpenParen:
                    Consume();
                    Expr group = Expression();
                    Consume(ETokenType.CloseParen);
                    return group;
            }

            Consume();
            _diagnostics.Report(ESeverity.Error, $"Unexpected token.", token.span);
            
            return new ExprError();
        }

        ExprNumber ParseNumber()
        {
            Token token = Consume(ETokenType.LiteralNemeric);
            string lexeme = Source.GetString(token.span);
            Value value;
            if (int.TryParse(lexeme, out int i))
            {
                value = new Value(i);
            }
            else if (double.TryParse(lexeme, out double d))
            {
                value = new Value(d);
            }
            else
            {
                value = new Value(double.NaN);
                _diagnostics.Report(ESeverity.Error, $"Could not parse number '{lexeme}'.", token.span);
            }
            return new ExprNumber(token, value);
        }

        //NodeExprLiteralIdentifier ParseIdentifier()
        //{
        //    Token token = Consume(ETokenType.LiteralId);
        //    string lexeme = Source.GetString(token.span);

        //    if (_scope.CurrentScope.Shadows(lexeme))
        //    {
        //        _diagnostics.Report(ESeverity.Warning, $"Variable '{lexeme}' shadows variable declared in outer scope.", token.span);
        //    }
        //}

    }
}
