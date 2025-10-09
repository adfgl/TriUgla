using System.Runtime.CompilerServices;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Nodes;
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
            return ParseLogicalOrExpression();
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
                INode right = ParseExponentiationExpression();
                left = new NodeBinary(left, token, right);
            }
            return left;
        }

        INode ParseUnaryExpression()
        {
            Token token = Peek();

            if (token.type == ETokenType.Plus)
            {
                Consume();
                return ParsePrimaryExpression();
            }

            if (token.type == ETokenType.Minus || token.type == ETokenType.Not)
            {
                Consume();
                return new NodeUnary(token, ParseUnaryExpression());
            }
            return ParsePrimaryExpression();
        }

        INode ParseRange()
        {
            Token tkOpen = Peek();
            var args = ParseArguments(ETokenType.OpenCurly, ETokenType.CloseCurly, ETokenType.Colon);

            switch (args.Count)
            {
                case 2:
                    return new NodeRangeLiteral(tkOpen, args[0], args[1], null);

                case 3:
                    return new NodeRangeLiteral(tkOpen, args[0], args[1], args[2]);

                default:
                    throw new Exception();
            }
        }

        INode ParseSimplePrimaryExpression()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.IdentifierLiteral:
           
                    return new NodeIdentifierLiteral(Consume());

                case ETokenType.NumericLiteral:
                    return new NodeNumericLiteral(Consume());

                case ETokenType.StringLiteral:
                    return new NodeStringLiteral(Consume());

                case ETokenType.OpenCurly:
                    return ParseRange();

                case ETokenType.OpenParen:
                    Token tkOpenParen = Consume(ETokenType.OpenParen);
                    INode expr = ParseExpression();
                    Consume(ETokenType.CloseParen);
                    return new NodeGroup(tkOpenParen, expr);
            }

            throw new Exception();
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

        INode ParseFunctionCall(Token callee)
        {
            var args = ParseArguments(ETokenType.OpenParen, ETokenType.CloseParen, ETokenType.Comma);
            return new NodeFun(callee, args); 
        }

        INode ParsePrimaryExpression()
        {
            INode expr = ParseSimplePrimaryExpression();
            return expr;
        }

        INode ParseDeclarationOrExprStatement()
        {
            var identTok = Peek();
            Consume(ETokenType.IdentifierLiteral);

            if (TryConsume(ETokenType.OpenSquare, out _))
            {
                Consume(ETokenType.CloseSquare);
                Consume(ETokenType.Equal);

                List<INode> values = ParseArguments(ETokenType.OpenCurly, ETokenType.CloseCurly);

                NodeTupleLiteral tuple = new NodeTupleLiteral(new Token(), values);

                return new NodeDeclarationOrAssignment(identTok, tuple);
            }
            else
            {
                if (TryConsume(ETokenType.Equal, out _))
                {
                    var rhs = ParseExpression();
                    return new NodeDeclarationOrAssignment(identTok, rhs);
                }
            }
            return new NodeIdentifierLiteral(identTok);
        }

        INode ParseFor()
        {
            ReadStop stop = new ReadStop(ETokenType.EndFor, ETokenType.EOF);

            Token tkFor = Consume(ETokenType.For);
            INode var = ParseExpression();
            Consume(ETokenType.In);
            INode rng = ParseExpression();

            NodeBlock forBlock = ParseBlockUntil(stop);

            NodeIdentifierLiteral? id = var as NodeIdentifierLiteral;
            if (id is null)
            {
                throw new Exception();
            }
            return new NodeFor(tkFor, id, rng, forBlock);
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

            Consume(ETokenType.EndIf);
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
                        throw new Exception();

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

                    case ETokenType.IdentifierLiteral:
                        if (Peek(1).type == ETokenType.OpenParen)
                        {
                            Consume(ETokenType.IdentifierLiteral);
                            INode fun = ParseFunctionCall(token);
                            statements.Add(fun);
                        }
                        else
                        {
                            statements.Add(ParseDeclarationOrExprStatement());
                        }
                        MaybeEOX();
                        break;

                    default:
                        throw new Exception();
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
                throw new Exception();
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
