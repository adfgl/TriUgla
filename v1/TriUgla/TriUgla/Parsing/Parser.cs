using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Expressions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Nodes.Statements;
using TriUgla.Parsing.Nodes.Statements.Conditional;

namespace TriUgla.Parsing
{
    public class Parser
    {
        readonly Scanner _scanner;

        public Parser(string source)
        {
            _scanner = new Scanner(source);
        }

        NodeExprBase ParseExpression()
        {
            return ParseLogicalOrExpression();
        }

        NodeExprBase ParseLogicalOrExpression()
        {
            NodeExprBase left = ParseLogicalAndExpression();
            while (Peek().type == ETokenType.Or)
            {
                Token token = Consume();
                NodeExprBase right = ParseLogicalAndExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeExprBase ParseLogicalAndExpression()
        {
            NodeExprBase left = ParseEqualityExpression();
            while (Peek().type == ETokenType.And)
            {
                Token token = Consume();
                NodeExprBase right = ParseEqualityExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeExprBase ParseEqualityExpression()
        {
            NodeExprBase left = ParseRelationalExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.EqualStrict &&
                    token.type != ETokenType.NotEqual)
                {
                    break;
                }
                Consume();
                NodeExprBase right = ParseRelationalExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeExprBase ParseRelationalExpression()
        {
            NodeExprBase left = ParseAdditiveExpression();
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
                NodeExprBase right = ParseAdditiveExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeExprBase ParseAdditiveExpression()
        {
            NodeExprBase left = ParseMultiplicativeExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Plus && 
                    token.type != ETokenType.Minus)
                {
                    break;
                }
                Consume();
                NodeExprBase right = ParseMultiplicativeExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeExprBase ParseMultiplicativeExpression()
        {
            NodeExprBase left = ParseExponentiationExpression();
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
                NodeExprBase right = ParseExponentiationExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        public ProgramNode Parse()
        {
            return new ProgramNode(ParseStatements());
        }

        NodeExprBase ParseExponentiationExpression()
        {
            NodeExprBase left = ParseUnaryExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Power)
                {
                    break;
                }
                Consume();
                NodeExprBase right = ParseExponentiationExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeExprBase ParseUnaryExpression()
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
                return new NodeExprUnary(token, ParseUnaryExpression());
            }
            return ParsePrimaryExpression();
        }

        NodeExprBase ParsePrimaryExpression()
        {
            NodeExprBase expr = ParseSimplePrimaryExpression();
            return expr;
        }

        NodeExprBase ParseSimplePrimaryExpression()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.IdentifierLiteral:
                    return new NodeExprIdentifierLiteral(Consume());

                case ETokenType.NumericLiteral:
                    return new NodeExprNumericLiteral(Consume());

                case ETokenType.StringLiteral:
                    return new NodeExprStringLiteral(Consume());

                case ETokenType.OpenParen:
                    Consume();
                    NodeExprBase expr = ParseExpression();
                    if (expr is not NodeExprLiteralBase)
                    {
                        expr = new NodeExprGroup(token, expr);
                    }
                    Consume(ETokenType.CloseParen);
                    return expr;
            }

            throw new UnexpectedTokenException(token);
        }

        List<NodeStmtBase> ParseStatements()
        {
            List<NodeStmtBase> statements = new List<NodeStmtBase>();

            Token token;
            while ((token = Peek()).type != ETokenType.EOF)
            {
                switch (token.type)
                {
                    case ETokenType.Error:
                        throw new RuntimeException(token);

                    case ETokenType.LineBreak:
                        Consume();
                        break;

                    case ETokenType.If:
                        statements.Add(ParseIfElseStatement());
                        MaybeEOX();
                        break;

                    case ETokenType.ElseIf:
                    case ETokenType.Else:
                    case ETokenType.EndIf:
                        return statements;

                    case ETokenType.StringLiteral:
                    case ETokenType.NumericLiteral:
                    case ETokenType.OpenParen:
                    case ETokenType.Plus:
                    case ETokenType.Minus:
                        if (ParseLiteralStatement(ParseExpression(), out NodeStmtExpression? literal))
                        {
                            statements.Add(literal);
                        }
                        break;

                    case ETokenType.IdentifierLiteral:
                        statements.Add(ParseDeclaration());
                        break;



                    default:
                        throw new UnexpectedTokenException(token);
                }
            }
            return statements;
        }

        NodeStmtAssignmentOrDeclaration ParseDeclaration()
        {
            NodeExprBase id = ParseExpression();
            if (id is not NodeExprIdentifierLiteral)
            {
                throw new UnexpectedTokenException(id.Token);
            }

            NodeExprBase? expression = null;
            if (TryConsume(ETokenType.Equal, out _))
            {
                expression = ParseExpression();
            }
            return new NodeStmtAssignmentOrDeclaration(id.Token, id, expression);
        }

        bool ParseLiteralStatement(NodeExprBase literal, out NodeStmtExpression stmt)
        {
            Token eoe = Peek();

            if (IsEOX(eoe))
            {
                Consume();
                stmt = new NodeStmtExpression(literal.Token, literal);
                return true;
            }
            throw new UnexpectedTokenException(eoe);
        }

        NodeStmtBase ParseIfElseStatement()
        {
            Token ifToken = Consume(ETokenType.If);

            Consume(ETokenType.OpenParen);
            NodeExprBase condition = ParseExpression();
            Consume(ETokenType.CloseParen);

            List<NodeStmtBase> ifStatements = new List<NodeStmtBase>();
            while (Peek().type != ETokenType.ElseIf && Peek().type != ETokenType.Else && Peek().type != ETokenType.EndIf && Peek().type != ETokenType.EOF)
            {
                ifStatements.AddRange(ParseStatements());
            }

            List<NodeStmtElif> elifs = new List<NodeStmtElif>();
            while (TryConsume(ETokenType.ElseIf, out Token elifToken))
            {
                Consume(ETokenType.OpenParen);
                NodeExprBase elifCondition = ParseExpression();
                Consume(ETokenType.CloseParen);

                List<NodeStmtBase> elifStatements = new List<NodeStmtBase>();
                while (Peek().type != ETokenType.ElseIf && Peek().type != ETokenType.Else && Peek().type != ETokenType.EndIf && Peek().type != ETokenType.EOF)
                {
                    elifStatements.AddRange(ParseStatements());
                }
                elifs.Add(new NodeStmtElif(elifToken, elifCondition, elifStatements.ToArray()));
            }

            NodeStmtBase[]? elseStatements = null;
            if (TryConsume(ETokenType.Else, out _))
            {
                elseStatements = ParseStatements().ToArray();
            }

            Consume(ETokenType.EndIf);
            return new NodeStmtIfElse(ifToken, condition, ifStatements, elifs, elseStatements);
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
                throw new UnexpectedTokenException(token, type);
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
                throw new UnexpectedTokenException(token);
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
