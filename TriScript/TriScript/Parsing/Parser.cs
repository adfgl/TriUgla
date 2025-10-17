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

        public Parser(Source source, DiagnosticBag diagnostic)
        {
            _scanner = new Scanner(source);
            _diagnostics = diagnostic;
        }

        public DiagnosticBag Diagnostics => _diagnostics;
        public Source Source => _scanner.Source;

        public NodeExprProgram Parse()
        {

        }

        Token Consume()
        {
            return _scanner.Consume(_diagnostics);
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

        NodeExprLiteralNumber ParseNumber()
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
            return new NodeExprLiteralNumber(token, value);
        }

        NodeExprLiteralIdentifier ParseIdentifier()
        {
            Token token = Consume(ETokenType.LiteralId);
            string lexeme = Source.GetString(token.span);

            if (CurrentScope.Shadows(content))
            {
                rt.Diagnostic.Report(ESeverity.Warning, $"Variable '{lexeme}' shadows variable declared in outer scope.", token.span);
            }
        }

        NodeExprBase ParseSimplePrimaryExpression()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.LiteralId:

                    break;

                case ETokenType.LiteralNemeric:
                    return ParseNumber();

                case ETokenType.LiteralString:
                    return new NodeExprLiteralString(Consume());

                case ETokenType.OpenParen:
                    return new NodeExprGroup(Consume(ETokenType.OpenParen), ParseExpression(), Consume(ETokenType.CloseParen));

            }
        }
    }
}
