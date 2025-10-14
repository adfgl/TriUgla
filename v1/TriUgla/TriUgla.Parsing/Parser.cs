using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.FlowControl;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Nodes.TupleOps;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Parser
    {
        readonly Scanner _scanner;

        public Parser(Scanner scanner)
        {
            _scanner = scanner;
        }

        public NodeBlock Parse()
        {
            return new NodeBlock(new Token(), ParseStatements());
        }

        INode ParseExpression()
        {
            return ParseAssignmentExpression();
        }

        // Lowest precedence, right-associative
        INode ParseAssignmentExpression()
        {
            INode left = ParseLogicalOrExpression();

            Token t = Peek();
            if (t.type == ETokenType.Equal
                || t.type == ETokenType.PlusEqual
                || t.type == ETokenType.MinusEqual
                || t.type == ETokenType.StarEqual
                || t.type == ETokenType.SlashEqual)
            {
                Token op = Consume(); // +=, -=, *=, ...
                INode right = ParseAssignmentExpression(); // right-assoc
                return new NodeAssignment(op, left, right);
            }

            return left;
        }

        INode ParseLogicalOrExpression()
        {
            INode left = ParseLogicalAndExpression();
            while (Peek().type == ETokenType.Or)
            {
                Token token = Consume();
                INode right = ParseLogicalAndExpression();
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseLogicalAndExpression()
        {
            INode left = ParseEqualityExpression();
            while (Peek().type == ETokenType.And)
            {
                Token token = Consume();
                INode right = ParseEqualityExpression();
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseEqualityExpression()
        {
            INode left = ParseRelationalExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.EqualEqual &&
                    token.type != ETokenType.NotEqual)
                {
                    break;
                }
                Consume();
                INode right = ParseRelationalExpression();
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseRelationalExpression()
        {
            INode left = ParseAdditiveExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Less &&
                    token.type != ETokenType.Greater &&
                    token.type != ETokenType.LessOrEqual &&
                    token.type != ETokenType.GreaterOrEqual)
                {
                    break;
                }
                Consume();
                INode right = ParseAdditiveExpression();
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseAdditiveExpression()
        {
            INode left = ParseMultiplicativeExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Plus &&
                    token.type != ETokenType.Minus)
                {
                    break;
                }
                Consume();
                INode right = ParseMultiplicativeExpression();
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseMultiplicativeExpression()
        {
            INode left = ParseExponentiationExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Star &&
                    token.type != ETokenType.Slash &&
                    token.type != ETokenType.Modulo)
                {
                    break;
                }
                Consume();
                INode right = ParseExponentiationExpression();
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseExponentiationExpression()
        {
            INode left = ParseUnaryExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Power)
                {
                    break;
                }
                Consume();
                INode right = ParseExponentiationExpression(); // right-assoc
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseUnaryExpression() // Unary (prefix ++/-- live here)
        {
            Token t = Peek();

            if (t.type == ETokenType.PlusPlus  // ++x
                || t.type == ETokenType.MinusMinus // --x
                || t.type == ETokenType.Plus   // +x
                || t.type == ETokenType.Minus  // -x
                || t.type == ETokenType.Not)   // !x
            {
                Token op = Consume();
                INode rhs = ParseUnaryExpression();
                return new NodePrefixUnary(op, rhs);
            }

            return ParsePostfixExpression();
        }

        INode ParsePostfixExpression()
        {
            INode expr = ParseSimplePrimaryExpression();

            while (true)
            {
                var t = Peek().type;

                if (t == ETokenType.OpenParen)
                {
                    var args = ParseArguments(ETokenType.OpenParen, ETokenType.CloseParen, ETokenType.Comma);
                    // Build a NodeFunctionCall with the function token first, per your convention
                    var id = (NodeIdentifier)expr;
                    expr = new NodeFunctionCall(id.Token, args);
                    continue;
                }

                if (t == ETokenType.OpenSquare)
                {
                    Token tkn = Consume(ETokenType.OpenSquare);
                    INode index = ParseExpression();
                    Consume(ETokenType.CloseSquare);
                    expr = new NodeValueAt(tkn, expr, index);
                    continue;
                }

                if (t == ETokenType.PlusPlus || t == ETokenType.MinusMinus)
                {
                    Token op = Consume();
                    expr = new NodePostfixUnary(op, expr);
                    continue;
                }

                break;
            }

            return expr;
        }

        INode ParseSimplePrimaryExpression()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.Hash:
                    {
                        Token hash = Consume();
                        if (Peek().type == ETokenType.IdentifierLiteral
                            || Peek().type == ETokenType.OpenParen
                            || Peek().type == ETokenType.OpenCurly
                            || Peek().type == ETokenType.StringLiteral
                            || Peek().type == ETokenType.NumericLiteral)
                        {
                            INode exp = ParsePostfixExpression(); // allow # on any primary/postfix chain
                            return new NodeLengthOf(hash, exp);
                        }
                        break;
                    }

                case ETokenType.IdentifierLiteral:
                    return new NodeIdentifier(Consume(), null);

                case ETokenType.Point:
                case ETokenType.Line:
                    Token pointTkn = Consume();

                    Consume(ETokenType.OpenParen);
                    INode pointNode = ParseExpression();
                    Consume(ETokenType.CloseParen);
                    return new NodeIdentifier(pointTkn, pointNode);

                case ETokenType.NumericLiteral:
                    return new NodeNumeric(Consume());

                case ETokenType.StringLiteral:
                    return new NodeString(Consume());

                case ETokenType.OpenCurly:
                    return ParseRangeOrTuple();

                case ETokenType.OpenParen:
                    {
                        Token tkOpenParen = Consume(ETokenType.OpenParen);
                        INode expr = ParseExpression();
                        Consume(ETokenType.CloseParen);
                        return new NodeGroup(tkOpenParen, expr);
                    }
            }

            throw new Exception("Unexpected token in primary: " + token.type);
        }



        INode ParseRangeOrTuple()
        {
            Token tkOpen = Peek();
            Consume(ETokenType.OpenCurly);

            if (Peek().type == ETokenType.CloseCurly)
            {
                Consume(ETokenType.CloseCurly);
                return new NodeTuple(tkOpen, new List<INode>(0));
            }

            var items = new List<INode>(4);

            items.Add(ParseExpression());

            var t = Peek().type;

            if (t == ETokenType.CloseCurly)
            {
                Consume(ETokenType.CloseCurly);
                return new NodeTuple(tkOpen, items);
            }

            ETokenType sep;
            if (t == ETokenType.Comma || t == ETokenType.Colon)
            {
                sep = t;
                Consume();
            }
            else
            {
                throw new Exception("Expected ',' or ':' or '}' after first element.");
            }

            while (true)
            {
                if (sep == ETokenType.Comma && Peek().type == ETokenType.CloseCurly)
                    break;

                var next = Peek().type;
                if (sep == ETokenType.Comma && next == ETokenType.Colon)
                    throw new Exception("Mixed separators: expected ',' consistently.");
                if (sep == ETokenType.Colon && next == ETokenType.Comma)
                    throw new Exception("Mixed separators: expected ':' consistently.");

                items.Add(ParseExpression());

                if (!TryConsume(sep, out _))
                    break;
            }

            Consume(ETokenType.CloseCurly);

            if (sep == ETokenType.Colon)
            {
                if (items.Count == 2)
                    return new NodeRange(tkOpen, items[0], items[1], null);
                if (items.Count == 3)
                    return new NodeRange(tkOpen, items[0], items[2], items[1]);
                throw new Exception("Range requires {start:end} or {start:step:end}.");
            }

            return new NodeTuple(tkOpen, items);
        }

        List<INode> ParseArguments(ETokenType open = ETokenType.OpenParen, ETokenType close = ETokenType.CloseParen, ETokenType separator = ETokenType.Comma)
        {
            Consume(open);
            List<INode> args = new List<INode>(4);
            if (Peek().type != close)
            {
                while (true)
                {
                    args.Add(ParseExpression());
                    if (!TryConsume(separator, out _)) break;
                    if (Peek().type == close) break;
                }
            }
            Consume(close);
            return args;
        }

        INode ParseFor()
        {
            ReadStop stop = new ReadStop(ETokenType.EndFor, ETokenType.EOF);

            Token tkFor = Consume(ETokenType.For);
            INode var = new NodeIdentifier(Consume(ETokenType.IdentifierLiteral), null);

            Consume(ETokenType.In);
            INode rng = ParseExpression();

            NodeBlock forBlock = ParseBlockUntil(stop);

            return new NodeFor(tkFor, var, rng, forBlock);
        }

        INode ParseIfElse()
        {
            ReadStop stop = new ReadStop(ETokenType.ElseIf, ETokenType.Else, ETokenType.EndIf, ETokenType.EOF);

            var tkIf = Consume(ETokenType.If);

            Consume(ETokenType.OpenParen);
            INode condition = ParseExpression();
            Consume(ETokenType.CloseParen);

            NodeBlock ifBlock = ParseBlockUntil(stop);

            List<(INode Cond, NodeBlock Block)> elifs = new List<(INode Cond, NodeBlock Block)>();
            while (TryConsume(ETokenType.ElseIf, out _))
            {
                Consume(ETokenType.OpenParen);
                INode elifCond = ParseExpression();
                Consume(ETokenType.CloseParen);

                NodeBlock elifBlock = ParseBlockUntil(stop);
                elifs.Add((elifCond, elifBlock));
            }

            NodeBlock? elseBlock = null;
            if (TryConsume(ETokenType.Else, out _))
            {
                elseBlock = ParseBlockUntil(new ReadStop(ETokenType.EndIf, ETokenType.EOF));
            }
            return new NodeIfElse(tkIf, condition, ifBlock, elifs, elseBlock);
        }

        NodeBlock ParseBlockUntil(ReadStop stop)
        {
            var stmts = new List<INode>(8);

            while (true)
            {
                var t = Peek().type;
                if (stop.Contains(t)) break;
                stmts.AddRange(ParseStatements());
                if (t == ETokenType.EOF) break;
            }
            return new NodeBlock(new Token(), stmts);
        }

        readonly struct ReadStop
        {
            private readonly ETokenType a, b, c, d;
            private readonly int count;
            public ReadStop(ETokenType a) { this.a = a; b = c = d = 0; count = 1; }
            public ReadStop(ETokenType a, ETokenType b) { this.a = a; this.b = b; c = d = 0; count = 2; }
            public ReadStop(ETokenType a, ETokenType b, ETokenType c) { this.a = a; this.b = b; this.c = c; d = 0; count = 3; }
            public ReadStop(ETokenType a, ETokenType b, ETokenType c, ETokenType d) { this.a = a; this.b = b; this.c = c; this.d = d; count = 4; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(ETokenType t)
            {
                if (t == a) return true;
                if (count > 1 && t == b) return true;
                if (count > 2 && t == c) return true;
                if (count > 3 && t == d) return true;
                return false;
            }
        }

        List<INode> ParseStatements()
        {
            List<INode> statements = new List<INode>();

            Token token;
            while ((token = Peek()).type != ETokenType.EOF)
            {
                switch (token.type)
                {
                    case ETokenType.Comment:
                    case ETokenType.MultiLineComment:
                    case ETokenType.LineBreak:
                        Consume();
                        break;

                    case ETokenType.Error:
                        throw new Exception(token.value);

                    case ETokenType.If:
                        statements.Add(ParseIfElse());
                        break;

                    case ETokenType.For:
                        statements.Add(ParseFor());
                        break;

                    case ETokenType.ElseIf:
                    case ETokenType.Else:
                    case ETokenType.EndIf:
                    case ETokenType.EndFor:
                        Consume();
                        return statements;

                    default:
                        {
                            INode expr = ParseExpression();
                            statements.Add(expr);
                            MaybeEOX();
                            break;
                        }
                }
            }
            return statements;
        }

        Token Consume()
        {
            return _scanner.Consume();
        }

        Token Peek(int offset = 0)
        {
            return _scanner.Peek(offset);
        }

        Token Consume(ETokenType type)
        {
            Token token = Peek();
            if (token.type != type)
            {
                throw new Exception($"Expected {type} but got {token.type}");
            }
            return Consume();
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

        bool IsEOX(Token token)
        {
            ETokenType type = token.type;
            if (type != ETokenType.EOF && type != ETokenType.LineBreak && type != ETokenType.SemiColon)
            {
                return false;
            }
            return true;
        }

        void EOX()
        {
            Token token = Peek();
            if (false == IsEOX(token))
            {
                throw new Exception();
            }
            Consume();
        }

        void MaybeEOX()
        {
            if (IsEOX(Peek()))
            {
                Consume();
            }
        }
    }

}
