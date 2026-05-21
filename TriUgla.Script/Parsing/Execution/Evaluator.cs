using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Data;
using TriUgla.Script.Data.Collections;
using TriUgla.Script.Parsing.Nodes;
using TriUgla.Script.Parsing.Nodes.Expressions;
using TriUgla.Script.Parsing.Nodes.Statements;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing.Execution
{
    public sealed class Evaluator : INodeVisitor<Value>
    {
        readonly ScopeStack<Value> _scopes = new();

        public ExecutionResult Result { get; } = new();

        public ExecutionResult Execute(StmtProg program)
        {
            try
            {
                Result.Value = program.Accept(this);
                Result.ExitCode = 0;
            }
            catch (ScriptExitException ex)
            {
                Result.HasExited = true;
                Result.ExitCode = ex.Code;
            }
            catch (Exception ex)
            {
                Result.Exception = ex;
                Result.ExitCode = -1;
            }

            return Result;
        }

        public Value Visit(StmtProg node)
        {
            _scopes.Open();

            Value last = Value.Undefined;

            foreach (Stmt stmt in node.Statements)
                last = stmt.Accept(this);

            return last;
        }

        public Value Visit(StmtBlock node)
        {
            using (_scopes.Guard())
            {
                Value last = Value.Undefined;

                foreach (Stmt stmt in node.Statements)
                    last = stmt.Accept(this);

                return last;
            }
        }

        public Value Visit(StmtExpr node)
        {
            return node.Expr.Accept(this);
        }

        public Value Visit(StmtAssign node)
        {
            Value value = node.Value.Accept(this);

            if (node.Target is ExprIdentifier id)
            {
                string name = Text(id.Token);

                if ((OperatorKind)node.Op.Value == OperatorKind.Assign)
                {
                    _scopes.Set(name, value);
                    return value;
                }

                Value current = _scopes.Get(name);

                Value result = (OperatorKind)node.Op.Value switch
                {
                    OperatorKind.PlusAssign => Value.Add(current, value),
                    OperatorKind.MinusAssign => Value.Subtract(current, value),
                    OperatorKind.MultiplyAssign => Value.Multiply(current, value),
                    OperatorKind.DivideAssign => Value.Divide(current, value),
                    _ => throw Runtime(node.Op, "Unsupported assignment operator.")
                };

                _scopes.Set(name, result);
                return result;
            }

            throw Runtime(node.Op, "Invalid assignment target.");
        }

        public Value Visit(StmtIf node)
        {
            if (node.Condition.Accept(this).AsBool())
                return node.ThenBlock.Accept(this);

            foreach (StmtElseIf elseIf in node.ElseIfs)
            {
                if (elseIf.Condition.Accept(this).AsBool())
                    return elseIf.Block.Accept(this);
            }

            return node.ElseBlock?.Accept(this) ?? Value.Undefined;
        }

        public Value Visit(StmtElseIf node)
        {
            return node.Block.Accept(this);
        }

        public Value Visit(StmtFor node)
        {
            Value iterable = node.Iterable.Accept(this);
            string name = Text(node.Variable);

            IEnumerable<Value> values = Enumerate(iterable, node.Variable);

            using (_scopes.Guard())
            {
                foreach (Value value in values)
                {
                    _scopes.Set(name, value);

                    try
                    {
                        node.Body.Accept(this);
                    }
                    catch (ScriptContinueException)
                    {
                        continue;
                    }
                    catch (ScriptBreakException)
                    {
                        break;
                    }
                }
            }

            return Value.Undefined;
        }

        public Value Visit(StmtWhile node)
        {
            while (node.Condition.Accept(this).AsBool())
            {
                try
                {
                    node.Body.Accept(this);
                }
                catch (ScriptContinueException)
                {
                    continue;
                }
                catch (ScriptBreakException)
                {
                    break;
                }
            }

            return Value.Undefined;
        }

        public Value Visit(StmtBreak node)
        {
            throw new ScriptBreakException();
        }

        public Value Visit(StmtContinue node)
        {
            throw new ScriptContinueException();
        }

        public Value Visit(StmtError node)
        {
            throw Runtime(node.Token, node.Message);
        }

        public Value Visit(ExprIdentifier node)
        {
            return _scopes.Get(Text(node.Token));
        }

        public Value Visit(ExprNumber node)
        {
            return Math.Abs(node.Value - Math.Round(node.Value)) < 1e-12
                ? new Value((int)node.Value)
                : new Value(node.Value);
        }

        public Value Visit(ExprString node)
        {
            return Value.FromString(node.Value);
        }

        public Value Visit(ExprBoolean node)
        {
            return new Value(node.Value);
        }

        public Value Visit(ExprGroup node)
        {
            return node.Inner.Accept(this);
        }

        public Value Visit(ExprUnary node)
        {
            Value right = node.Right.Accept(this);

            return (OperatorKind)node.Op.Value switch
            {
                OperatorKind.Plus => right,
                OperatorKind.Minus => Value.Negate(right),
                OperatorKind.Not => Value.Not(right),
                _ => throw Runtime(node.Op, "Unsupported unary operator.")
            };
        }

        public Value Visit(ExprBinary node)
        {
            Value left = node.Left.Accept(this);

            OperatorKind op = (OperatorKind)node.Op.Value;

            if (op == OperatorKind.And)
                return new Value(left.AsBool() && node.Right.Accept(this).AsBool());

            if (op == OperatorKind.Or)
                return new Value(left.AsBool() || node.Right.Accept(this).AsBool());

            Value right = node.Right.Accept(this);

            return op switch
            {
                OperatorKind.Plus => Value.Add(left, right),
                OperatorKind.Minus => Value.Subtract(left, right),
                OperatorKind.Multiply => Value.Multiply(left, right),
                OperatorKind.Divide => Value.Divide(left, right),
                OperatorKind.Modulo => Value.Modulo(left, right),
                OperatorKind.Power => Value.Power(left, right),

                OperatorKind.Equal => Value.Equal(left, right),
                OperatorKind.NotEqual => Value.NotEqual(left, right),
                OperatorKind.Less => Value.Less(left, right),
                OperatorKind.LessEqual => Value.LessEqual(left, right),
                OperatorKind.Greater => Value.Greater(left, right),
                OperatorKind.GreaterEqual => Value.GreaterEqual(left, right),

                _ => throw Runtime(node.Op, "Unsupported binary operator.")
            };
        }

        public Value Visit(ExprList node)
        {
            List<Value> values = new(node.Items.Count);

            foreach (Expr item in node.Items)
                values.Add(item.Accept(this));

            return Value.FromList(values);
        }

        public Value Visit(ExprRange node)
        {
            Value start = node.Start.Accept(this);
            Value end = node.End.Accept(this);
            Value? step = node.Step?.Accept(this);

            return Value.FromRange(start, end, step);
        }

        public Value Visit(ExprIndex node)
        {
            Value target = node.Target.Accept(this);
            int index = node.Index.Accept(this).AsInt();

            if (target.Object is ObjList list)
                return list.Values[index];

            if (target.Object is ObjString str)
                return Value.FromString(str.Content[index].ToString());

            throw Runtime(node.Open, "Value is not indexable.");
        }

        public Value Visit(ExprCall node)
        {
            string name = Text(node.Name);

            return name switch
            {
                "print" => Print(node),
                "exit" => Exit(node),
                _ => throw Runtime(node.Name, $"Unknown function '{name}'.")
            };
        }

        public Value Visit(ExprError node)
        {
            throw Runtime(node.Token, node.Message);
        }

        Value Print(ExprCall node)
        {
            foreach (Expr arg in node.Args)
            {
                Value value = arg.Accept(this);
                Result.Log.Add(value.ToString());
            }

            return Value.Undefined;
        }

        Value Exit(ExprCall node)
        {
            if (node.Args.Count != 1)
                throw Runtime(node.Name, "exit() expects exactly one integer argument.");

            Value code = node.Args[0].Accept(this);

            throw new ScriptExitException(code.AsInt());
        }

        IEnumerable<Value> Enumerate(Value value, Token at)
        {
            if (value.Object is ObjRange range)
                return range.Enumerate();

            if (value.Object is ObjList list)
                return list.Values;

            throw Runtime(at, "Expected range or list.");
        }

        static string Text(Token token)
        {
            return token.Source.Slice(token.Span);
        }

        static Exception Runtime(Token token, string message)
        {
            return new InvalidOperationException(
                $"Runtime error at {token.Position}: {message}");
        }
    }
}
