using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Data;
using TriUgla.Script.Parsing.Nodes;
using TriUgla.Script.Parsing.Nodes.Expressions;
using TriUgla.Script.Parsing.Nodes.Statements;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Execution
{
    public sealed class TypeChecker(Diagnostics diagnostics) : INodeVisitor<ObjType>
    {
        readonly ScopeStack<ObjType> _scopes = new ScopeStack<ObjType>();
        readonly LoopTrack _loops = new LoopTrack();
        readonly Diagnostics _diagnostics = diagnostics;

        public ObjType Visit(StmtProg node)
        {
            _scopes.Open();

            ObjType last = ObjType.Undefined;

            foreach (Stmt stmt in node.Statements)
                last = stmt.Accept(this);

            return last;
        }

        public ObjType Visit(StmtBlock node)
        {
            using (_scopes.Guard())
            {
                ObjType last = ObjType.Undefined;

                foreach (Stmt stmt in node.Statements)
                    last = stmt.Accept(this);

                return last;
            }
        }

        public ObjType Visit(StmtExpr node)
            => node.Expr.Accept(this);

        public ObjType Visit(StmtAssign node)
        {
            ObjType valueType = node.Value.Accept(this);

            if (node.Target is ExprIdentifier id)
            {
                string name = Text(id.Token);

                if (_scopes.TryResolve(name, out var scope))
                {
                    ObjType current = scope[name];

                    if (!CanAssign(current, valueType))
                    {
                        Report(
                            node.Op,
                            $"Cannot assign {valueType} to '{name}' of type {current}.");
                    }

                    scope[name] = MergeAssignment(current, valueType);
                }
                else
                {
                    _scopes.Current.Declare(name, valueType);
                }

                return valueType;
            }

            if (node.Target is ExprIndex index)
            {
                ObjType targetType = index.Target.Accept(this);
                ObjType indexType = index.Index.Accept(this);

                if (indexType != ObjType.Integer)
                    Report(index.Open, $"Index must be Integer, got {indexType}.");

                if (!targetType.IsList)
                {
                    Report(index.Open, $"Type {targetType} is not index-assignable.");
                    return ObjType.Undefined;
                }

                ObjType elementType = targetType.Element ?? ObjType.Undefined;

                if (!CanAssign(elementType, valueType))
                {
                    Report(
                        node.Op,
                        $"Cannot assign {valueType} to list element of type {elementType}.");
                }

                return valueType;
            }

            Report(node.Op, "Left side of assignment must be identifier or index access.");
            return ObjType.Undefined;
        }

        public ObjType Visit(StmtIf node)
        {
            CheckCondition(node.Condition, "If");

            node.ThenBlock.Accept(this);

            foreach (StmtElseIf elseIf in node.ElseIfs)
                elseIf.Accept(this);

            node.ElseBlock?.Accept(this);

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtElseIf node)
        {
            CheckCondition(node.Condition, "ElseIf");
            node.Block.Accept(this);

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtFor node)
        {
            ObjType iterableType = node.Iterable.Accept(this);

            ObjType itemType;

            if (iterableType == ObjType.Range)
            {
                itemType = ObjType.Integer;
            }
            else if (iterableType.IsList)
            {
                itemType = iterableType.Element ?? ObjType.Undefined;
            }
            else
            {
                Report(node.Variable, $"For expects Range or List, got {iterableType}.");
                itemType = ObjType.Undefined;
            }

            using (_scopes.Guard())
            {
                _scopes.Current.Declare(Text(node.Variable), itemType);

                _loops.Enter();
                node.Body.Accept(this);
                _loops.Exit();
            }

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtWhile node)
        {
            CheckCondition(node.Condition, "While");

            _loops.Enter();
            node.Body.Accept(this);
            _loops.Exit();

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtBreak node)
        {
            if (!_loops.IsInsideLoop)
                Report(node.Token, "'Break' can only be used inside a loop.");

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtContinue node)
        {
            if (!_loops.IsInsideLoop)
                Report(node.Token, "'Continue' can only be used inside a loop.");

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtError node)
        {
            Report(node.Token, node.Message);
            return ObjType.Undefined;
        }

        public ObjType Visit(ExprIdentifier node)
        {
            string name = Text(node.Token);

            if (_scopes.TryGet(name, out ObjType type))
                return type;

            Report(node.Token, $"Unknown identifier '{name}'.");
            return ObjType.Undefined;
        }

        public ObjType Visit(ExprNumber node)
        {
            return IsIntegerLiteral(node.Token)
                ? ObjType.Integer
                : ObjType.Double;
        }

        public ObjType Visit(ExprString node)
            => ObjType.String;

        public ObjType Visit(ExprBoolean node)
            => ObjType.Boolean;

        public ObjType Visit(ExprGroup node)
            => node.Inner.Accept(this);

        public ObjType Visit(ExprUnary node)
        {
            ObjType right = node.Right.Accept(this);
            OperatorKind op = (OperatorKind)node.Op.Value;

            return op switch
            {
                OperatorKind.Plus => CheckUnaryNumber(node.Op, right, "+"),
                OperatorKind.Minus => CheckUnaryNumber(node.Op, right, "-"),
                OperatorKind.Not => CheckUnaryBoolean(node.Op, right),

                _ => Error(node.Op, $"Unsupported unary operator '{op}'.")
            };
        }

        public ObjType Visit(ExprBinary node)
        {
            OperatorKind op = (OperatorKind)node.Op.Value;

            if (op is OperatorKind.And or OperatorKind.Or)
            {
                ObjType left = node.Left.Accept(this);
                ObjType right = node.Right.Accept(this);

                if (left != ObjType.Boolean || right != ObjType.Boolean)
                {
                    Report(
                        node.Op,
                        $"Logical operator '{op}' expects Boolean operands, got {left} and {right}.");
                }

                return ObjType.Boolean;
            }

            ObjType l = node.Left.Accept(this);
            ObjType r = node.Right.Accept(this);

            return op switch
            {
                OperatorKind.Plus => BinaryPlus(node.Op, l, r),

                OperatorKind.Minus or
                OperatorKind.Multiply or
                OperatorKind.Modulo or
                OperatorKind.Power => BinaryNumber(node.Op, l, r, op),

                OperatorKind.Divide => BinaryDivide(node.Op, node.Right, l, r),

                OperatorKind.Less or
                OperatorKind.LessEqual or
                OperatorKind.Greater or
                OperatorKind.GreaterEqual => BinaryCompare(node.Op, l, r, op),

                OperatorKind.Equal or
                OperatorKind.NotEqual => BinaryEquality(node.Op, l, r),

                _ => Error(node.Op, $"Unsupported binary operator '{op}'.")
            };
        }

        public ObjType Visit(ExprList node)
        {
            if (node.Items.Count == 0)
                return ObjType.ListOf(ObjType.Undefined);

            ObjType element = node.Items[0].Accept(this);

            for (int i = 1; i < node.Items.Count; i++)
                element = CommonType(element, node.Items[i].Accept(this));

            return ObjType.ListOf(element);
        }

        public ObjType Visit(ExprRange node)
        {
            ObjType start = node.Start.Accept(this);
            ObjType end = node.End.Accept(this);

            if (!start.IsNumber)
                Report(GetToken(node.Start), $"Range start must be numeric, got {start}.");

            if (!end.IsNumber)
                Report(GetToken(node.End), $"Range end must be numeric, got {end}.");

            if (node.Step is not null)
            {
                ObjType step = node.Step.Accept(this);

                if (!step.IsNumber)
                    Report(GetToken(node.Step), $"Range step must be numeric, got {step}.");
            }

            return ObjType.Range;
        }

        public ObjType Visit(ExprIndex node)
        {
            ObjType target = node.Target.Accept(this);
            ObjType index = node.Index.Accept(this);

            if (index != ObjType.Integer)
                Report(node.Open, $"Index must be Integer, got {index}.");

            if (target.IsList)
                return target.Element ?? ObjType.Undefined;

            if (target == ObjType.String)
                return ObjType.String;

            Report(node.Open, $"Type {target} is not indexable.");
            return ObjType.Undefined;
        }

        public ObjType Visit(ExprError node)
        {
            Report(node.Token, node.Message);
            return ObjType.Undefined;
        }

        void CheckCondition(Expr condition, string owner)
        {
            ObjType type = condition.Accept(this);

            if (type != ObjType.Boolean)
                Report(GetToken(condition), $"{owner} condition must be Boolean, got {type}.");
        }

        ObjType BinaryPlus(Token op, ObjType left, ObjType right)
        {
            if (left == ObjType.String || right == ObjType.String)
                return ObjType.String;

            return BinaryNumber(op, left, right, OperatorKind.Plus);
        }

        ObjType BinaryNumber(Token op, ObjType left, ObjType right, OperatorKind kind)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                Report(op, $"Operator '{kind}' expects numbers, got {left} and {right}.");
                return ObjType.Undefined;
            }

            if (kind == OperatorKind.Power)
                return ObjType.Double;

            return PromoteNumber(left, right);
        }

        ObjType BinaryDivide(Token op, Expr rightExpr, ObjType left, ObjType right)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                Report(op, $"Operator '/' expects numbers, got {left} and {right}.");
                return ObjType.Undefined;
            }

            if (IsLiteralZero(rightExpr))
                Report(op, "Division by constant zero.");

            return ObjType.Double;
        }

        ObjType BinaryCompare(Token op, ObjType left, ObjType right, OperatorKind kind)
        {
            if (!left.IsNumber || !right.IsNumber)
                Report(op, $"Comparison '{kind}' expects numbers, got {left} and {right}.");

            return ObjType.Boolean;
        }

        ObjType BinaryEquality(Token op, ObjType left, ObjType right)
        {
            if (left.IsNumber && right.IsNumber)
                return ObjType.Boolean;

            if (left == right)
                return ObjType.Boolean;

            Report(op, $"Cannot compare {left} and {right}.");
            return ObjType.Boolean;
        }

        ObjType CheckUnaryNumber(Token op, ObjType right, string symbol)
        {
            if (!right.IsNumber)
            {
                Report(op, $"Unary '{symbol}' expects number, got {right}.");
                return ObjType.Undefined;
            }

            return right;
        }

        ObjType CheckUnaryBoolean(Token op, ObjType right)
        {
            if (right != ObjType.Boolean)
                Report(op, $"Unary '!' expects Boolean, got {right}.");

            return ObjType.Boolean;
        }

        ObjType Error(Token token, string message)
        {
            Report(token, message);
            return ObjType.Undefined;
        }

        static ObjType PromoteNumber(ObjType a, ObjType b)
        {
            return a == ObjType.Double || b == ObjType.Double
                ? ObjType.Double
                : ObjType.Integer;
        }

        static ObjType CommonType(ObjType a, ObjType b)
        {
            if (a == b)
                return a;

            if (a.IsNumber && b.IsNumber)
                return PromoteNumber(a, b);

            if (a.IsList && b.IsList)
            {
                ObjType ae = a.Element ?? ObjType.Undefined;
                ObjType be = b.Element ?? ObjType.Undefined;

                return ObjType.ListOf(CommonType(ae, be));
            }

            return ObjType.Undefined;
        }

        static bool CanAssign(ObjType expected, ObjType actual)
        {
            if (expected == actual)
                return true;

            if (expected == ObjType.Double && actual == ObjType.Integer)
                return true;

            if (expected.IsList && actual.IsList)
            {
                ObjType expectedElement = expected.Element ?? ObjType.Undefined;
                ObjType actualElement = actual.Element ?? ObjType.Undefined;

                return CanAssign(expectedElement, actualElement);
            }

            return false;
        }

        static ObjType MergeAssignment(ObjType current, ObjType next)
        {
            if (CanAssign(current, next))
                return current;

            return CommonType(current, next);
        }

        static bool IsLiteralZero(Expr expr)
        {
            return expr is ExprNumber n && Math.Abs(n.Value) < 1e-12;
        }

        bool IsIntegerLiteral(Token token)
        {
            return int.TryParse(Text(token), out _);
        }

        Token GetToken(Expr expr)
        {
            return expr switch
            {
                ExprIdentifier x => x.Token,
                ExprNumber x => x.Token,
                ExprString x => x.Token,
                ExprBoolean x => x.Token,
                ExprUnary x => x.Op,
                ExprBinary x => x.Op,
                ExprGroup x => x.Open,
                ExprList x => x.Open,
                ExprRange x => GetToken(x.Start),
                ExprIndex x => x.Open,
                ExprError x => x.Token,
                _ => default
            };
        }

        string Text(Token token)
        {
            return token.Source.Slice(token.Span);
        }

        void Report(Token token, string message)
        {
            _diagnostics.Report(
                Severity.Error,
                message,
                token);
        }
    }
}
