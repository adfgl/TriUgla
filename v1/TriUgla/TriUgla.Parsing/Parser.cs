using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Parser
    {
        readonly Scanner _scanner;

        public Parser(string source)
        {
            _scanner = new Scanner(source);
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

                case ETokenType.OpenParen:
                    Consume();
                    INode expr = ParseExpression();
                    if (expr is not NodeIdentifierLiteral)
                    {
                        expr = new NodeGroup(expr);
                    }
                    Consume(ETokenType.CloseParen);
                    return expr;
            }

            throw new Exception();
        }

        INode[] ParseArguments(ETokenType open = ETokenType.OpenParen, ETokenType close = ETokenType.CloseParen, ETokenType separator = ETokenType.Comma)
        {
            Consume(open);
            List<INode> arguments = new List<INode>();
            while (Peek().type != ETokenType.CloseParen)
            {
                arguments.Add(ParseExpression());
                if (false == TryConsume(separator, out _))
                {
                    break;
                }
            }
            Consume(close);
            return arguments.ToArray();
        }

        INode ParsePrimaryExpression()
        {
            INode expr = ParseSimplePrimaryExpression();
            NodeIdentifierLiteral? id = expr as NodeIdentifierLiteral;

            bool chaining = id is not null;
            while (chaining)
            {
                Token token = Peek();

                switch (token.type)
                {
                    case ETokenType.OpenParen:
                        INode[] args = ParseArguments();
                        expr = new NodeFun(id!.Token, args);
                        break;

                    default:
                        chaining = false;
                        break;
                }
            }
            return expr;
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
            if (type != ETokenType.EOF && type != ETokenType.LineBreak && type != ETokenType.Colon && type != ETokenType.SemiColon)
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

        void Skip()
        {
            while (true)
            {
                var token = Peek().type;
                if (token != ETokenType.LineBreak || token != ETokenType.Comment)
                {
                    break;
                }
                Consume();
            }
        }
    }
}
