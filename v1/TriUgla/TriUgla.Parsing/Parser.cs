using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
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

        public ProgramExprNode Parse()
        {
            return new ProgramExprNode(new Token(), ParseStatements());
        }

        List<NodeBase> ParseStatements()
        {
            List<NodeBase> statements = new List<NodeBase>();

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
                        throw new CompileTimeException(token.value, token);

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
                    case ETokenType.EndMacro:
                        return statements;

                    case ETokenType.Macro:
                        statements.Add(ParseMacro());
                        break;

                    case ETokenType.Call:
                        statements.Add(ParseMacroCall());
                        break;

                    default:
                        statements.Add(ParseExpression());
                        MaybeEOX();
                        break;
                }
            }
            return statements;
        }

        NodeBase ParseExpression()
        {
            return ParseAssignmentExpression();
        }

        NodeBase ParseAssignmentExpression()
        {
            NodeBase left = ParseConditionalExpression();

            Token t = Peek();
            if (t.type == ETokenType.Equal
                || t.type == ETokenType.PlusEqual
                || t.type == ETokenType.MinusEqual
                || t.type == ETokenType.StarEqual
                || t.type == ETokenType.SlashEqual)
            {
                Token op = Consume();                     // =, +=, -=, *=, /=
                NodeBase right = ParseAssignmentExpression(); // right-assoc
                return new NodeExprAssignment(left, op, right);
            }

            return left;
        }

        NodeBase ParseConditionalExpression()
        {
            NodeBase condition = ParseLogicalOrExpression();
            if (Peek().type == ETokenType.Question)
            {
                Token q = Consume(); // '?'
                NodeBase thenExpr = ParseAssignmentExpression();
                Token c = Consume(ETokenType.Colon); // ':'
                NodeBase elseExpr = ParseConditionalExpression();
                return new NodeExprTernary(condition, q, thenExpr, c, elseExpr);
            }
            return condition;
        }

        NodeBase ParseLogicalOrExpression()
        {
            NodeBase left = ParseLogicalAndExpression();
            while (Peek().type == ETokenType.Or)
            {
                Token token = Consume();
                NodeBase right = ParseLogicalAndExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeBase ParseLogicalAndExpression()
        {
            NodeBase left = ParseEqualityExpression();
            while (Peek().type == ETokenType.And)
            {
                Token token = Consume();
                NodeBase right = ParseEqualityExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeBase ParseEqualityExpression()
        {
            NodeBase left = ParseRelationalExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.EqualEqual &&
                    token.type != ETokenType.NotEqual)
                {
                    break;
                }
                Consume();
                NodeBase right = ParseRelationalExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeBase ParseRelationalExpression()
        {
            NodeBase left = ParseAdditiveExpression();
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
                NodeBase right = ParseAdditiveExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeBase ParseAdditiveExpression()
        {
            NodeBase left = ParseMultiplicativeExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Plus &&
                    token.type != ETokenType.Minus)
                {
                    break;
                }
                Consume();
                NodeBase right = ParseMultiplicativeExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeBase ParseMultiplicativeExpression()
        {
            NodeBase left = ParseExponentiationExpression();
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
                NodeBase right = ParseExponentiationExpression();
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeBase ParseExponentiationExpression()
        {
            NodeBase left = ParseUnaryExpression();
            while (true)
            {
                Token token = Peek();
                if (token.type != ETokenType.Power)
                {
                    break;
                }
                Consume();
                NodeBase right = ParseExponentiationExpression(); // right-assoc
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeBase ParseUnaryExpression() // Unary (prefix ++/-- live here)
        {
            Token t = Peek();

            if (t.type == ETokenType.PlusPlus  // ++x
                || t.type == ETokenType.MinusMinus // --x
                || t.type == ETokenType.Plus   // +x
                || t.type == ETokenType.Minus  // -x
                || t.type == ETokenType.Not)   // !x
            {
                Token op = Consume();
                NodeBase rhs = ParseUnaryExpression();
                return new NodeExprPrefixUnary(op, rhs);
            }

            return ParsePostfixExpression();
        }

        NodeBase ParsePostfixExpression()
        {
            NodeBase expr = ParseSimplePrimaryExpression();

            while (true)
            {
                ETokenType t = Peek().type;
                if (t == ETokenType.OpenParen)
                {
                    if (expr is not NodeExprIdentifier id)
                    {
                        throw new Exception();
                    }

                    List<NodeBase> args = ParseArguments(ETokenType.OpenParen, ETokenType.CloseParen, ETokenType.Comma);
                    expr = new NodeExprFunctionCall(expr.Token, id, args);
                    continue;
                }

                if (t == ETokenType.OpenSquare)
                {
                    Token tkn = Consume(ETokenType.OpenSquare);
            
                    NodeBase index = ParseExpression();
                    Consume(ETokenType.CloseSquare);
                    expr = new NodeExprValueAt(tkn, expr, index);
                    continue;
                }

                if (t == ETokenType.PlusPlus || t == ETokenType.MinusMinus)
                {
                    Token op = Consume();
                    expr = new NodeExprPostfixUnary(op, expr);
                    continue;
                }

                break;
            }

            return expr;
        }

        NodeBase ParseSimplePrimaryExpression()
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
                            NodeBase exp = ParsePostfixExpression(); 
                            return new NodeExprLengthOf(hash, exp);
                        }
                        break;
                    }

                case ETokenType.IdentifierLiteral:
                    Token id = Consume();

                    if (Peek(0).type == ETokenType.OpenSquare &&
                        Peek(1).type == ETokenType.CloseSquare)
                    {
                        Consume(ETokenType.OpenSquare);
                        Consume(ETokenType.CloseSquare);
                        return new NodeExprIdentifierTuple(id);
                    }
                    return new NodeExprIdentifier(id);

                case ETokenType.NumericLiteral:
                    return new NodeExprNumeric(Consume());

                case ETokenType.StringLiteral:
                    return new NodeExprString(Consume());

                case ETokenType.OpenCurly:
                    return ParseRangeOrTuple();

                case ETokenType.OpenParen:
                    Token tkOpenParen = Consume(ETokenType.OpenParen);
                    NodeBase expr = ParseExpression();
                    return new NodeExprGroup(tkOpenParen, expr, Consume(ETokenType.CloseParen));
            }

            throw new Exception("Unexpected token in primary: " + token.type);
        }

        NodeBase ParseRangeOrTuple()
        {
            Token tkOpen = Peek();
            Consume(ETokenType.OpenCurly);

            if (Peek().type == ETokenType.CloseCurly)
            {
                return new NodeExprTuple(tkOpen, [], Consume(ETokenType.CloseCurly));
            }

            var items = new List<NodeBase>(4);

            items.Add(ParseExpression());

            ETokenType t = Peek().type;
            if (t == ETokenType.CloseCurly)
            {
                return new NodeExprTuple(tkOpen, items, Consume(ETokenType.CloseCurly));
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

            Token tkClose = Consume(ETokenType.CloseCurly);

            if (sep == ETokenType.Colon)
            {
                return new NodeExprRange(tkOpen, items, tkClose);
            }
            return new NodeExprTuple(tkOpen, items, tkClose);
        }

        List<NodeBase> ParseArguments(ETokenType open = ETokenType.OpenParen, ETokenType close = ETokenType.CloseParen, ETokenType separator = ETokenType.Comma)
        {
            Consume(open);
            List<NodeBase> args = new List<NodeBase>(4);
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

        NodeBase ParseFor()
        {
            Token tkFor = Consume(ETokenType.For);
            NodeBase counter = ParseExpression();
            Consume(ETokenType.In);
            NodeBase range = ParseExpression();
            NodeStmtBlock forBlock = ParseBlockUntil(new Token(), [ETokenType.EndFor, ETokenType.EOF]);
            return new NodeStmtFor(tkFor, counter, range, forBlock, Consume(ETokenType.EndFor));
        }

        NodeBase ParseIfElse()
        {
            HashSet<ETokenType> stop = [ETokenType.ElseIf, ETokenType.Else, ETokenType.EndIf, ETokenType.EOF];

            List<(NodeBase Cond, NodeStmtBlock Block)> elifs = new List<(NodeBase Cond, NodeStmtBlock Block)>();

            Token tkIf = Consume(ETokenType.If);
            Consume(ETokenType.OpenParen);
            NodeBase condition = ParseExpression();
            Consume(ETokenType.CloseParen);
            NodeStmtBlock ifBlock = ParseBlockUntil(tkIf, stop);

            elifs.Add((condition, ifBlock));

            while (TryConsume(ETokenType.ElseIf, out var tkElseIf))
            {
                Consume(ETokenType.OpenParen);
                NodeBase elifCond = ParseExpression();
                Consume(ETokenType.CloseParen);

                NodeStmtBlock elifBlock = ParseBlockUntil(tkElseIf, stop);
                elifs.Add((elifCond, elifBlock));
            }

            NodeStmtBlock? elseBlock = null;
            if (TryConsume(ETokenType.Else, out var tkElse))
            {
                elseBlock = ParseBlockUntil(tkElse, [ETokenType.EndIf, ETokenType.EOF]);
            }

            return new NodeStmtIfElse(tkIf, elifs, elseBlock, Consume(ETokenType.EndIf));
        }

        NodeStmtBlock ParseBlockUntil(Token token, HashSet<ETokenType> stop)
        {
            List<NodeBase> stmts = new List<NodeBase>(8);
            while (true)
            {
                var t = Peek().type;
                if (t == ETokenType.EOF || stop.Contains(t))
                    break;

                stmts.AddRange(ParseStatements());
            }
            return new NodeStmtBlock(token, stmts);
        }

        NodeBase ParseMacro()
        {
            Token tkMacro = Consume();
            NodeBase nameExpr = ParseExpression();
            NodeStmtBlock block = ParseBlockUntil(tkMacro, [ETokenType.EndMacro, ETokenType.EOF]);
            return new NodeStmtMacro(tkMacro, nameExpr, block, Consume(ETokenType.EndMacro));
        }

        NodeBase ParseMacroCall()
        {
            Token tkCall = Consume(ETokenType.Call);
            NodeBase nameExpr = ParseExpression();
            MaybeEOX();
            return new NodeStmtMacroCall(tkCall, nameExpr);
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
                throw new CompileTimeException($"Unexpected token: Expected {type} but got {token.type} ({token.value}).", token);
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
