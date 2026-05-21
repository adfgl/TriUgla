using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Parsing.Nodes;
using TriUgla.Script.Parsing.Nodes.Expressions;
using TriUgla.Script.Parsing.Nodes.Statements;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Execution
{
    public sealed class UsageChecker : INodeVisitor<bool>
    {
        sealed class UsageInfo
        {
            public Token Token { get; }
            public bool Read { get; set; }
            public bool Written { get; set; }

            public UsageInfo(Token token)
            {
                Token = token;
            }
        }

        readonly ScopeStack<UsageInfo> _scopes = new();
        readonly Diagnostics _diagnostics;

        public UsageChecker(Diagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public bool Visit(StmtProg node)
        {
            using (_scopes.Guard())
            {
                foreach (Stmt stmt in node.Statements)
                    stmt.Accept(this);

                FlushCurrentScope();
            }

            return true;
        }

        public bool Visit(StmtBlock node)
        {
            using (_scopes.Guard())
            {
                foreach (Stmt stmt in node.Statements)
                    stmt.Accept(this);

                FlushCurrentScope();
            }

            return true;
        }

        public bool Visit(StmtExpr node)
        {
            node.Expr.Accept(this);
            return true;
        }

        public bool Visit(StmtAssign node)
        {
            node.Value.Accept(this);

            if (node.Target is ExprIdentifier id)
            {
                string name = Text(id.Token);

                if (!_scopes.TryResolve(name, out _))
                {
                    _scopes.Current.Declare(name, new UsageInfo(id.Token));
                }

                if ((OperatorKind)node.Op.Value == OperatorKind.Assign)
                {
                    MarkWritten(name);
                }
                else
                {
                    MarkRead(name);
                    MarkWritten(name);
                }

                return true;
            }

            if (node.Target is ExprIndex index)
            {
                index.Target.Accept(this);
                index.Index.Accept(this);

                if (index.Target is ExprIdentifier container)
                {
                    string name = Text(container.Token);
                    MarkRead(name);
                    MarkWritten(name);
                }

                return true;
            }

            node.Target.Accept(this);
            return true;
        }

        public bool Visit(StmtIf node)
        {
            node.Condition.Accept(this);
            node.ThenBlock.Accept(this);

            foreach (StmtElseIf elseIf in node.ElseIfs)
                elseIf.Accept(this);

            node.ElseBlock?.Accept(this);

            return true;
        }

        public bool Visit(StmtElseIf node)
        {
            node.Condition.Accept(this);
            node.Block.Accept(this);
            return true;
        }

        public bool Visit(StmtFor node)
        {
            node.Iterable.Accept(this);

            using (_scopes.Guard())
            {
                string name = Text(node.Variable);

                _scopes.Current.Declare(name, new UsageInfo(node.Variable)
                {
                    Written = true
                });

                node.Body.Accept(this);

                FlushCurrentScope();
            }

            return true;
        }

        public bool Visit(StmtWhile node)
        {
            node.Condition.Accept(this);
            node.Body.Accept(this);
            return true;
        }

        public bool Visit(StmtBreak node) => true;

        public bool Visit(StmtContinue node) => true;

        public bool Visit(StmtError node) => true;

        public bool Visit(ExprIdentifier node)
        {
            MarkRead(Text(node.Token));
            return true;
        }

        public bool Visit(ExprNumber node) => true;

        public bool Visit(ExprString node) => true;

        public bool Visit(ExprBoolean node) => true;

        public bool Visit(ExprUnary node)
        {
            node.Right.Accept(this);
            return true;
        }

        public bool Visit(ExprBinary node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            return true;
        }

        public bool Visit(ExprGroup node)
        {
            node.Inner.Accept(this);
            return true;
        }

        public bool Visit(ExprList node)
        {
            foreach (Expr item in node.Items)
                item.Accept(this);

            return true;
        }

        public bool Visit(ExprRange node)
        {
            node.Start.Accept(this);
            node.End.Accept(this);
            node.Step?.Accept(this);

            return true;
        }

        public bool Visit(ExprIndex node)
        {
            node.Target.Accept(this);
            node.Index.Accept(this);

            return true;
        }

        public bool Visit(ExprError node) => true;

        void MarkRead(string name)
        {
            if (_scopes.TryResolve(name, out var scope))
                scope[name].Read = true;
        }

        void MarkWritten(string name)
        {
            if (_scopes.TryResolve(name, out var scope))
                scope[name].Written = true;
        }

        void FlushCurrentScope()
        {
            foreach ((string name, UsageInfo info) in _scopes.Current.Variables)
            {
                if (!info.Read && !info.Written)
                {
                    _diagnostics.Warning(
                        $"Variable '{name}' is declared but never used.",
                        info.Token);
                }
                else if (!info.Read && info.Written)
                {
                    _diagnostics.Warning(
                        $"Variable '{name}' is written but never read.",
                        info.Token);
                }
            }
        }

        static string Text(Token token)
        {
            return token.Source.Slice(token.Span);
        }
    }
}
