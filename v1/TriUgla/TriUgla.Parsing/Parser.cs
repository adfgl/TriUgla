using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes;
using TriUgla.Parsing.Nodes.Expressions;
using TriUgla.Parsing.Nodes.Expressions.Literals;
using TriUgla.Parsing.Nodes.Statements;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing
{
    public class Parser 
    {
        int _loopDepth;
        readonly Scanner _scanner;

        public Parser(Scanner scanner)
        {
            _scanner = scanner;
        }

        public NodeExprProgram Parse()
        {
            Token t = new Token();
            return new NodeExprProgram(t, new NodeStmtBlock(t, ParseStatements()));
        }

        List<NodeBase> ParseStatements()
        {
            List<NodeBase> statements = new List<NodeBase>();
            
            Token token;
            while ((token = Peek()).type != ETokenType.EOF)
            {
                switch (token.type)
                {
                    case ETokenType.Error:
                        throw new CompileTimeException(token.value, token);

                    case ETokenType.Comment:
                    case ETokenType.MultiLineComment:
                    case ETokenType.LineBreak:
                        Consume();
                        break;

                    case ETokenType.Identifier:
                        NodeExprBase exp = ParseExpression();
                        if (exp is NodeExprAssignment or NodeExprAssignmentCompound or NodeExprPostfixUnary or NodeExprPrefixUnary)
                        {
                            statements.Add(new NodeStmtExpression(token, exp));
                        }
                        else
                        {
                            throw new CompileTimeException($"Unexpected '{token.value}'", token);
                        }
                        break;

                    case ETokenType.Print:
                        Token print = Consume(ETokenType.Print);
                        Consume(ETokenType.OpenParen);

                        NodeBase? expr = null;
                        if (!TryConsume(ETokenType.CloseParen, out _))
                        {
                            expr = ParseExpression();
                            Consume(ETokenType.CloseParen);
                        }
                        MaybeEOX();

                        statements.Add(new NodeStmtPrint(print, expr));
                        break;

                    case ETokenType.If:
                        HashSet<ETokenType> stop = [ETokenType.ElseIf, ETokenType.Else, ETokenType.EndIf, ETokenType.EOF];

                        List<(NodeBase Cond, NodeStmtBlock Block)> elifs = new List<(NodeBase Cond, NodeStmtBlock Block)>();

                        Token tkIf = Consume(ETokenType.If);
                        NodeBase condition = ParseExpression();
                        NodeStmtBlock ifBlock = ParseBlockUntil(tkIf, stop);

                        elifs.Add((condition, ifBlock));

                        while (TryConsume(ETokenType.ElseIf, out var tkElseIf))
                        {
                            NodeBase elifCond = ParseExpression();

                            NodeStmtBlock elifBlock = ParseBlockUntil(tkElseIf, stop);
                            elifs.Add((elifCond, elifBlock));
                        }

                        NodeStmtBlock? elseBlock = null;
                        if (TryConsume(ETokenType.Else, out var tkElse))
                        {
                            elseBlock = ParseBlockUntil(tkElse, [ETokenType.EndIf, ETokenType.EOF]);
                        }
                        statements.Add(new NodeStmtIfElse(tkIf, elifs, elseBlock, Consume(ETokenType.EndIf)));
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
                        Token tkMacro = Consume();
                        NodeBase macroNameExpr = ParseExpression();
                        NodeStmtBlock block = ParseBlockUntil(tkMacro, [ETokenType.EndMacro, ETokenType.EOF]);
                        statements.Add(new NodeStmtMacro(tkMacro, macroNameExpr, block, Consume(ETokenType.EndMacro)));
                        break;

                    case ETokenType.Call:
                        Token tkCall = Consume(ETokenType.Call);
                        NodeExprBase nameExpr = ParseExpression();
                        MaybeEOX();

                        statements.Add(new NodeStmtMacroCall(tkCall, nameExpr));
                        break;

                    case ETokenType.Abort:
                        statements.Add(new NodeStmtAbort(Consume()));
                        MaybeEOX();
                        break;

                    case ETokenType.Return:
                        Token tkReturn = Consume();
                        Consume(ETokenType.OpenParen);
                        NodeExprBase? returnExpr = null;
                        if (!TryConsume(ETokenType.CloseParen, out _))
                        {
                            returnExpr = ParseExpression();
                            Consume(ETokenType.CloseParen);
                        }
                        statements.Add(new NodeStmtReturn(tkReturn, returnExpr));
                        break;

                    case ETokenType.Break:
                        Token tkBreak = Consume(); MaybeEOX();
                        if (_loopDepth == 0)
                        {
                            throw new CompileTimeException($"'{tkBreak.value}' used outside of loop.", tkBreak);
                        }
                        statements.Add(new NodeStmtBreak(tkBreak));
                        break;

                    case ETokenType.Continue:
                        Token tkContinue = Consume(); MaybeEOX();
                        if (_loopDepth == 0)
                        {
                            throw new CompileTimeException($"'{tkContinue.value}' used outside of loop.", tkContinue);
                        }
                        statements.Add(new NodeStmtContinue(tkContinue));
                        break;

                    default:
                        throw new CompileTimeException($"Unexpected '{token.value}'", token);
                }
            }
            return statements;
        }

        public NodeExprBase ParseExpression()
        {
            return ParseAssignmentExpression();
        }

        NodeExprBase ParseAssignmentExpression()
        {
            NodeExprBase left = ParseConditionalExpression();

            Token t = Peek();
            if (t.type == ETokenType.Equal
                || t.type == ETokenType.PlusEqual
                || t.type == ETokenType.MinusEqual
                || t.type == ETokenType.StarEqual
                || t.type == ETokenType.SlashEqual)
            {
                Token op = Consume();
                NodeExprBase right = ParseAssignmentExpression();
                if (op.type == ETokenType.Equal)
                {
                    return new NodeExprAssignment(left, op, right);
                }
                else
                {
                    return new NodeExprAssignmentCompound(left, op, right);
                }
            }
            return left;
        }

        NodeExprBase ParseConditionalExpression()
        {
            NodeExprBase condition = ParseLogicalOrExpression();
            if (Peek().type == ETokenType.Question)
            {
                Token q = Consume(); // '?'
                NodeExprBase thenExpr = ParseAssignmentExpression();
                Token c = Consume(ETokenType.Colon); // ':'
                NodeExprBase elseExpr = ParseConditionalExpression();
                return new NodeExprTernary(condition, q, thenExpr, c, elseExpr);
            }
            return condition;
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
                if (token.type != ETokenType.EqualEqual &&
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
                NodeExprBase right = ParseExponentiationExpression(); // right-assoc
                left = new NodeExprBinary(left, token, right);
            }
            return left;
        }

        NodeExprBase ParseUnaryExpression() // Unary (prefix ++/-- live here)
        {
            Token t = Peek();

            if (t.type == ETokenType.PlusPlus  // ++x
                || t.type == ETokenType.MinusMinus // --x
                || t.type == ETokenType.Plus   // +x
                || t.type == ETokenType.Minus  // -x
                || t.type == ETokenType.Not)   // !x
            {
                Token op = Consume();
                NodeExprBase rhs = ParseUnaryExpression();
                return new NodeExprPrefixUnary(op, rhs);
            }

            return ParsePostfixExpression();
        }

        NodeExprBase ParsePostfixExpression()
        {
            NodeExprBase expr = ParseSimplePrimaryExpression();

            while (true)
            {
                ETokenType t = Peek().type;
                if (t == ETokenType.OpenParen)
                {
                    throw new Exception();

                }

                if (t == ETokenType.OpenSquare)
                {
                    Token tkn = Consume(ETokenType.OpenSquare);

                    NodeExprBase index = ParseExpression();
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

        NodeExprNameOf ParseNameOf()
        {
            Token tknNameOf = Consume(ETokenType.NameOf);
            Consume(ETokenType.OpenParen);
            if (TryConsume(ETokenType.CloseParen, out _))
            {
                throw new CompileTimeException("Missing argument", tknNameOf);
            }

            NodeExprBase exp = ParseExpression();
            Consume(ETokenType.CloseParen);
            MaybeEOX();

            if (exp is NodeExprIdentifier id)
            {
                return new NodeExprNameOf(tknNameOf, id);
            }
            else
            {
                throw new CompileTimeException("Expected identifier", exp.Token);
            }
        }

        NodeExprLengthOf ParseHash()
        {
            Token hash = Consume();
            NodeExprBase exp = ParsePostfixExpression();
            return new NodeExprLengthOf(hash, exp);
        }

        NodeExprIdentifier ParseIdentifier()
        {
            Token id = Consume();

            bool isTuple = false;
            if (Peek(0).type == ETokenType.OpenSquare &&
              Peek(1).type == ETokenType.CloseSquare)
            {
                Consume(ETokenType.OpenSquare);
                Consume(ETokenType.CloseSquare);
                isTuple = true;
            }

            NodeExprBase? index = null;
            if (TryConsume(ETokenType.Tilda, out _))
            {
                Token open = Consume(ETokenType.OpenCurly);
                if (TryConsume(ETokenType.CloseCurly, out _))
                {
                    throw new CompileTimeException("Missing index", open);
                }
                else
                {
                    index = ParseExpression();
                    Consume(ETokenType.CloseCurly);
                }
            }
            return new NodeExprIdentifier(id, isTuple, index);
        }

        NodeExprBase ParseSimplePrimaryExpression()
        {
            Token token = Peek();
            switch (token.type)
            {
                case ETokenType.NameOf:
                    return ParseNameOf();

                case ETokenType.NativeFunction:
                    Consume();
                    List<NodeExprBase> args = ParseArguments(ETokenType.OpenParen, ETokenType.CloseParen, ETokenType.Comma);
                    return new NodeExprFunctionCall(token, args);

                case ETokenType.Hash:
                    return ParseHash();

                case ETokenType.Identifier:
                    return ParseIdentifier();

                case ETokenType.Numeric:
                    return new NodeExprNumeric(Consume());

                case ETokenType.String:
                    return new NodeExprString(Consume());

                case ETokenType.OpenCurly:
                    return ParseRangeOrTuple();

                case ETokenType.OpenParen:
                    return new NodeExprGroup(Consume(ETokenType.OpenParen), ParseExpression(), Consume(ETokenType.CloseParen));

            }

            throw new CompileTimeException("Unexpected token in primary: " + token.type, token);
        }

        NodeExprBase ParseRangeOrTuple()
        {
            Token tkOpen = Peek();
            Consume(ETokenType.OpenCurly);

            if (Peek().type == ETokenType.CloseCurly)
            {
                return new NodeExprTuple(tkOpen, [], Consume(ETokenType.CloseCurly));
            }

            var items = new List<NodeExprBase>(4);

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

        List<NodeExprBase> ParseArguments(ETokenType open = ETokenType.OpenParen, ETokenType close = ETokenType.CloseParen, ETokenType separator = ETokenType.Comma)
        {
            Consume(open);
            List<NodeExprBase> args = new List<NodeExprBase>(4);
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

            bool forIn;
            List<NodeExprBase> args;
            if (Peek().type == ETokenType.OpenParen)
            {
                args = ParseArguments(ETokenType.OpenParen, ETokenType.CloseParen, ETokenType.Colon);
                forIn = false;
            }
            else
            {
                NodeExprBase counter = ParseExpression();
                Consume(ETokenType.In);
                NodeExprBase range = ParseExpression();
                args = [counter, range];
                forIn = true;
            }

            NodeStmtBlock forBlock = ParseBlockUntil(tkFor, [ETokenType.EndFor, ETokenType.EOF]);
            Token end = Consume(ETokenType.EndFor);

            if (forIn)
            {
                return new NodeStmtForIn(tkFor, args[0], args[1], forBlock, end);
            }
            return new NodeStmtFor(tkFor, args, forBlock);
        }

        public NodeStmtBlock ParseBlockUntil(Token token, HashSet<ETokenType> stop)
        {
            _loopDepth++;
            List<NodeBase> stmts = new List<NodeBase>(8);
            while (true)
            {
                var t = Peek().type;
                if (t == ETokenType.EOF || stop.Contains(t))
                    break;

                stmts.AddRange(ParseStatements());
            }
            _loopDepth--;
            return new NodeStmtBlock(token, stmts);
        }

        public Token Consume()
        {
            return _scanner.Consume();
        }

        public Token Peek(int offset = 0)
        {
            return _scanner.Peek(offset);
        }

        public Token Consume(ETokenType type)
        {
            Token token = Peek();
            if (token.type != type)
            {
                throw new CompileTimeException($"Unexpected token: Expected '{type}' but got '{token.type}' ({token.value}).", token);
            }
            return Consume();
        }

        public bool TryConsume(ETokenType type, out Token token)
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
            if (type != ETokenType.EOF &&
                type != ETokenType.LineBreak && 
                type != ETokenType.SemiColon)
            {
                return false;
            }
            return true;
        }

        public void MaybeEOX()
        {
            if(IsEOX(Peek())) Consume();
        }
    }
}
