using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Data;
using TriUgla.Script.Data.Collections;
using TriUgla.Script.Data.Geometry;
using TriUgla.Script.Parsing.Execution;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public sealed class ScriptExitException(int code) : Exception
    {
        public int Code { get; } = code;
    }

    public sealed class ScriptBreakException : Exception;

    public sealed class ScriptContinueException : Exception;

    public sealed class Evaluator : INodeVisitor<Value>
    {
        readonly ScopeStack<Value> _scopes = new();

        public ExecutionResult Result { get; } = new();

        ScriptContext Context => Result.Context;

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
            => node.Expression.Accept(this);

        public Value Visit(StmtAssign node)
        {
            Value value = node.Value.Accept(this);

            if (node.Target is ExprIdentifier id)
            {
                AssignName(id.Token.Text, node.Op, value);
                return value;
            }

            if (node.Target is ExprIndex index)
                return AssignIndex(index, node.Op, value);

            if (node.Target is ExprMember member)
                return AssignMember(member, node.Op, value);

            throw Runtime(node.Op, "Invalid assignment target.");
        }

        public Value Visit(StmtIf node)
        {
            if (node.Condition.Accept(this).AsBool())
                return node.ThenBranch.Accept(this);

            foreach (StmtElseIf item in node.ElseIfs)
            {
                if (item.Condition.Accept(this).AsBool())
                    return item.Body.Accept(this);
            }

            return node.ElseBranch?.Accept(this) ?? Value.Undefined;
        }

        public Value Visit(StmtElseIf node)
            => node.Body.Accept(this);

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
                }
                catch (ScriptBreakException)
                {
                    break;
                }
            }

            return Value.Undefined;
        }

        public Value Visit(StmtFor node)
        {
            Value iterable = node.Iterable.Accept(this);

            using (_scopes.Guard())
            {
                foreach (Value value in Enumerate(iterable, node.Identifier))
                {
                    _scopes.Set(node.Identifier.Text, value);

                    try
                    {
                        node.Body.Accept(this);
                    }
                    catch (ScriptContinueException)
                    {
                    }
                    catch (ScriptBreakException)
                    {
                        break;
                    }
                }
            }

            return Value.Undefined;
        }

        public Value Visit(StmtBreak node)
            => throw new ScriptBreakException();

        public Value Visit(StmtContinue node)
            => throw new ScriptContinueException();

        public Value Visit(StmtReturn node)
            => node.Value?.Accept(this) ?? Value.Undefined;

        public Value Visit(StmtPoint node)
        {
            int id = EvalInt(node.Id);
            List<Value> v = EvalList(node.Values);

            RequireCount(v, 4, GetToken(node.Values), "Point expects {x, y, z, meshSize}.");

            ObjPoint point = new(
                new PointId(id),
                v[0].AsDouble(),
                v[1].AsDouble(),
                v[2].AsDouble(),
                v[3].AsDouble());

            return Register(point);
        }

        public Value Visit(StmtLine node)
        {
            int id = EvalInt(node.Id);
            List<int> ids = EvalIntList(node.Values);

            RequireCount(ids, 2, GetToken(node.Values), "Line expects {startPointId, endPointId}.");

            return Register(new ObjLine(
                new CurveId(id),
                new PointId(ids[0]),
                new PointId(ids[1])));
        }

        public Value Visit(StmtCircle node)
        {
            int id = EvalInt(node.Id);
            List<int> ids = EvalIntList(node.Values);

            RequireCount(ids, 3, GetToken(node.Values), "Circle expects {start, center, end}.");

            return Register(new ObjCircle(
                new CurveId(id),
                new PointId(ids[0]),
                new PointId(ids[1]),
                new PointId(ids[2])));
        }

        public Value Visit(StmtEllipse node)
        {
            int id = EvalInt(node.Id);
            List<int> ids = EvalIntList(node.Values);

            RequireCount(ids, 4, GetToken(node.Values), "Ellipse expects {start, center, major, end}.");

            return Register(new ObjEllipse(
                new CurveId(id),
                new PointId(ids[0]),
                new PointId(ids[1]),
                new PointId(ids[2]),
                new PointId(ids[3])));
        }

        public Value Visit(StmtSpline node)
        {
            int id = EvalInt(node.Id);
            List<PointId> ids = EvalPointIdList(node.Values);
            return Register(new ObjSpline(new CurveId(id), ids));
        }

        public Value Visit(StmtBSpline node)
        {
            int id = EvalInt(node.Id);
            List<PointId> ids = EvalPointIdList(node.Values);
            return Register(new ObjBSpline(new CurveId(id), ids));
        }

        public Value Visit(StmtBezier node)
        {
            int id = EvalInt(node.Id);
            List<PointId> ids = EvalPointIdList(node.Values);
            return Register(new ObjBezier(new CurveId(id), ids));
        }

        public Value Visit(StmtCurveLoop node)
        {
            int id = EvalInt(node.Id);
            List<int> raw = EvalIntList(node.Values);

            List<OrientedCurveId> curves = new(raw.Count);

            foreach (int item in raw)
                curves.Add(OrientedCurveId.FromSigned(item));

            return Register(new ObjCurveLoop(new CurveLoopId(id), curves));
        }

        public Value Visit(StmtPlaneSurface node)
        {
            int id = EvalInt(node.Id);
            List<int> raw = EvalIntList(node.Values);

            List<CurveLoopId> loops = new(raw.Count);

            foreach (int item in raw)
                loops.Add(new CurveLoopId(item));

            return Register(new ObjPlaneSurface(new SurfaceId(id), loops));
        }

        public Value Visit(StmtPhysical node)
        {
            int id = EvalInt(node.Id);
            List<int> ids = EvalIntList(node.Values);

            string? name = node.Name is null
                ? null
                : node.Name.Accept(this).AsString();

            PhysicalKind kind = node.Kind switch
            {
                Keyword.Point => PhysicalKind.Point,
                Keyword.Line or Keyword.Curve => PhysicalKind.Curve,
                Keyword.Surface => PhysicalKind.Surface,
                _ => throw Runtime(GetToken(node.Values), "Unsupported physical group kind.")
            };

            return Register(new ObjPhysicalGroup(
                new PhysicalGroupId(id),
                kind,
                name,
                ids));
        }

        public Value Visit(StmtTransfiniteCurve node)
        {
            List<int> raw = EvalIntList(node.Curves);

            List<CurveId> curves = new(raw.Count);

            foreach (int id in raw)
                curves.Add(new CurveId(id));

            int divisions = EvalInt(node.Divisions);
            double progression = node.Progression.Accept(this).AsDouble();

            return Register(new ObjTransfiniteCurve(curves, divisions, progression));
        }

        public Value Visit(StmtTransfiniteSurface node)
        {
            List<int> raw = EvalIntList(node.Surfaces);

            List<SurfaceId> surfaces = new(raw.Count);

            foreach (int id in raw)
                surfaces.Add(new SurfaceId(id));

            List<PointId> corners = [];

            if (node.Corners is not null)
                corners = EvalPointIdList(node.Corners);

            return Register(new ObjTransfiniteSurface(surfaces, corners));
        }

        public Value Visit(StmtRecombineSurface node)
        {
            List<int> raw = EvalIntList(node.Surfaces);

            List<SurfaceId> surfaces = new(raw.Count);

            foreach (int id in raw)
                surfaces.Add(new SurfaceId(id));

            return Register(new ObjRecombineSurface(surfaces));
        }

        public Value Visit(StmtEmbed node)
        {
            EntityKind2D entityKind = ToEntityKind(node.Entity);
            EntityKind2D containerKind = ToEntityKind(node.Container);

            List<int> entities = EvalIntList(node.Entities);
            List<int> containers = EvalIntList(node.Containers);

            return Register(new ObjEmbedConstraint(
                entityKind,
                entities,
                containerKind,
                containers));
        }

        public Value Visit(StmtMeshOption node)
        {
            Value value = node.Value.Accept(this);
            string path = string.Join(".", node.Path.Select(x => x.Text));

            ObjMeshOption option = new(path, value);
            return Register(option);
        }

        public Value Visit(StmtMeshCommand node)
        {
            string name = string.Join(" ", node.Path.Select(x => x.Text));

            List<Value> args = new(node.Args.Count);

            foreach (Expr arg in node.Args)
                args.Add(arg.Accept(this));

            ObjMeshCommand command = new(name, args);
            return Register(command);
        }

        public Value Visit(StmtError node)
            => throw Runtime(node.Token, node.Message);

        public Value Visit(ExprIdentifier node)
            => _scopes.Get(node.Token.Text);

        public Value Visit(ExprNumber node)
        {
            return Math.Abs(node.Value - Math.Round(node.Value)) < 1e-12
                ? new Value((int)Math.Round(node.Value))
                : new Value(node.Value);
        }

        public Value Visit(ExprString node)
            => Value.FromString(Unquote(node.Value));

        public Value Visit(ExprBoolean node)
            => new(node.Value);

        public Value Visit(ExprGroup node)
            => node.Inner.Accept(this);

        public Value Visit(ExprUnary node)
        {
            Value right = node.Right.Accept(this);

            if (node.Op.Is(OperatorKind.Plus))
                return right;

            if (node.Op.Is(OperatorKind.Minus))
                return right.Negate();

            if (node.Op.Is(OperatorKind.Not) || node.Op.Is(Keyword.Not))
                return right.Not();

            throw Runtime(node.Op, $"Unsupported unary operator '{node.Op.Text}'.");
        }

        public Value Visit(ExprBinary node)
        {
            Value left = node.Left.Accept(this);

            if (node.Op.Is(OperatorKind.And) || node.Op.Is(Keyword.And))
            {
                if (!left.AsBool())
                    return new Value(false);

                return new Value(node.Right.Accept(this).AsBool());
            }

            if (node.Op.Is(OperatorKind.Or) || node.Op.Is(Keyword.Or))
            {
                if (left.AsBool())
                    return new Value(true);

                return new Value(node.Right.Accept(this).AsBool());
            }

            Value right = node.Right.Accept(this);

            if (node.Op.Is(OperatorKind.Plus)) return left.Add(right);
            if (node.Op.Is(OperatorKind.Minus)) return left.Subtract(right);
            if (node.Op.Is(OperatorKind.Multiply)) return left.Multiply(right);
            if (node.Op.Is(OperatorKind.Divide)) return left.Divide(right);
            if (node.Op.Is(OperatorKind.Modulo)) return left.Modulo(right);
            if (node.Op.Is(OperatorKind.Power)) return left.Power(right);

            if (node.Op.Is(OperatorKind.Equal) || node.Op.Is(Keyword.Is)) return left.EqualTo(right);
            if (node.Op.Is(OperatorKind.NotEqual)) return left.NotEqualTo(right);

            if (node.Op.Is(OperatorKind.Less)) return left.LessThan(right);
            if (node.Op.Is(OperatorKind.LessEqual)) return left.LessEqual(right);
            if (node.Op.Is(OperatorKind.Greater)) return left.GreaterThan(right);
            if (node.Op.Is(OperatorKind.GreaterEqual)) return left.GreaterEqual(right);

            throw Runtime(node.Op, $"Unsupported binary operator '{node.Op.Text}'.");
        }

        public Value Visit(ExprTernary node)
            => node.Condition.Accept(this).AsBool()
                ? node.WhenTrue.Accept(this)
                : node.WhenFalse.Accept(this);

        public Value Visit(ExprCall node)
        {
            if (node.Target is not ExprIdentifier id)
                throw Runtime(node.Open, "Only simple function calls are supported.");

            string name = id.Token.Text;

            return name switch
            {
                "print" => Print(node),
                "exit" => Exit(node),
                "Transpose" => Transpose(node),
                "Inverse" => Inverse(node),
                _ => throw Runtime(id.Token, $"Unknown function '{name}'.")
            };
        }

        public Value Visit(ExprList node)
        {
            List<Value> values = new(node.Values.Count);

            foreach (Expr item in node.Values)
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

            if (node.Index is null)
                return target;

            int index = node.Index.Accept(this).AsInt();

            if (target.Object is ObjList list)
            {
                if ((uint)index >= (uint)list.Values.Count)
                    throw Runtime(node.Open, $"List index {index} is out of range.");

                return list.Values[index];
            }

            if (target.Object is ObjString str)
            {
                if ((uint)index >= (uint)str.Content.Length)
                    throw Runtime(node.Open, $"String index {index} is out of range.");

                return Value.FromString(str.Content[index].ToString());
            }

            throw Runtime(node.Open, $"Type '{target.Kind}' is not indexable.");
        }

        public Value Visit(ExprMember node)
        {
            string path = ToMemberPath(node);

            if (Context.Mesh.Options.TryGetValue(path, out Value value))
                return value;

            throw Runtime(node.Dot, $"Unknown member '{path}'.");
        }

        public Value Visit(ExprError node)
            => throw Runtime(node.Token, node.Message);

        void AssignName(string name, Token op, Value value)
        {
            if (op.Is(OperatorKind.Assign))
            {
                _scopes.Set(name, value);
                return;
            }

            Value current = _scopes.Get(name);

            Value result =
                op.Is(OperatorKind.PlusAssign) ? current.Add(value) :
                op.Is(OperatorKind.MinusAssign) ? current.Subtract(value) :
                op.Is(OperatorKind.MultiplyAssign) ? current.Multiply(value) :
                op.Is(OperatorKind.DivideAssign) ? current.Divide(value) :
                throw Runtime(op, "Unsupported assignment operator.");

            _scopes.Set(name, result);
        }

        Value AssignIndex(ExprIndex index, Token op, Value value)
        {
            if (index.Target is not ExprIdentifier id)
                throw Runtime(index.Open, "Index assignment target must be identifier.");

            string name = id.Token.Text;

            Value target = _scopes.Get(name);

            if (target.Object is not ObjList list)
                throw Runtime(index.Open, $"Variable '{name}' is not a list.");

            if (index.Index is null)
            {
                if (op.Is(OperatorKind.Assign))
                {
                    _scopes.Set(name, value);
                    return value;
                }

                if (op.Is(OperatorKind.PlusAssign))
                {
                    if (value.Object is ObjList incoming)
                        list.Values.AddRange(incoming.Values);
                    else
                        list.Values.Add(value);

                    return target;
                }

                throw Runtime(op, "Only '=' and '+=' are supported for empty index assignment.");
            }

            int i = index.Index.Accept(this).AsInt();

            if ((uint)i >= (uint)list.Values.Count)
                throw Runtime(index.Open, $"List index {i} is out of range.");

            if (!op.Is(OperatorKind.Assign))
                throw Runtime(op, "Indexed assignment only supports '='.");

            list.Values[i] = value;
            return value;
        }

        Value AssignMember(ExprMember member, Token op, Value value)
        {
            if (!op.Is(OperatorKind.Assign))
                throw Runtime(op, "Member assignment only supports '='.");

            string path = ToMemberPath(member);

            Context.Mesh.SetOption(path, value);

            return Register(new ObjMeshOption(path, value));
        }

        Value Register(Obj obj)
        {
            switch (obj)
            {
                case ObjPoint point:
                    Context.Geometry.Add(point);
                    break;

                case ObjCurve curve:
                    Context.Geometry.Add(curve);
                    break;

                case ObjCurveLoop loop:
                    Context.Geometry.Add(loop);
                    break;

                case ObjPlaneSurface surface:
                    Context.Geometry.Add(surface);
                    break;

                case ObjPhysicalGroup group:
                    Context.Geometry.Add(group);
                    break;

                case ObjTransfiniteCurve item:
                    Context.Geometry.Add(item);
                    break;

                case ObjTransfiniteSurface item:
                    Context.Geometry.Add(item);
                    break;

                case ObjRecombineSurface item:
                    Context.Geometry.Add(item);
                    break;

                case ObjEmbedConstraint item:
                    Context.Geometry.Add(item);
                    break;

                case ObjMeshOption option:
                    Context.Mesh.SetOption(option.Path, option.Value);
                    break;

                case ObjMeshCommand command:
                    Context.Mesh.AddCommand(command);
                    break;
            }

            return Value.FromObject(obj);
        }

        Value Print(ExprCall node)
        {
            foreach (Expr arg in node.Args)
                Context.Log.Add(arg.Accept(this).ToString());

            return Value.Undefined;
        }

        Value Exit(ExprCall node)
        {
            if (node.Args.Count != 1)
                throw Runtime(node.Open, "exit(...) expects one integer argument.");

            throw new ScriptExitException(node.Args[0].Accept(this).AsInt());
        }

        Value Transpose(ExprCall node)
        {
            if (node.Args.Count != 1)
                throw Runtime(node.Open, "Transpose(...) expects one argument.");

            return node.Args[0].Accept(this).Transpose();
        }

        Value Inverse(ExprCall node)
        {
            if (node.Args.Count != 1)
                throw Runtime(node.Open, "Inverse(...) expects one argument.");

            return node.Args[0].Accept(this).Inverse();
        }

        IEnumerable<Value> Enumerate(Value value, Token token)
        {
            if (value.Object is IObjEnumerable enumerable)
                return enumerable.Enumerate();

            throw Runtime(token, $"Type '{value.Kind}' is not enumerable.");
        }

        int EvalInt(Expr expr)
            => expr.Accept(this).AsInt();

        List<Value> EvalList(Expr expr)
        {
            Token at = GetToken(expr);

            Value value = expr.Accept(this);

            if (value.Object is ObjList list)
                return list.Values;

            if (value.Object is ObjRange range)
                return [.. range.Enumerate()];

            throw Runtime(at, $"Expected list or range, got {value.Kind}.");
        }

        List<int> EvalIntList(Expr expr)
        {
            List<Value> values = EvalList(expr);
            List<int> result = new(values.Count);

            foreach (Value value in values)
                result.Add(value.AsInt());

            return result;
        }

        List<PointId> EvalPointIdList(Expr expr)
        {
            List<int> values = EvalIntList(expr);
            List<PointId> result = new(values.Count);

            foreach (int value in values)
                result.Add(new PointId(value));

            return result;
        }

        static void RequireCount<T>(IReadOnlyList<T> values, int count, Token at, string message)
        {
            if (values.Count != count)
                throw Runtime(at, message);
        }

        static EntityKind2D ToEntityKind(Keyword keyword)
        {
            return keyword switch
            {
                Keyword.Point => EntityKind2D.Point,
                Keyword.Line or Keyword.Curve => EntityKind2D.Curve,
                Keyword.Surface => EntityKind2D.Surface,
                _ => throw new InvalidOperationException($"Unsupported entity kind {keyword}.")
            };
        }

        static string ToMemberPath(Expr expr)
        {
            if (expr is ExprIdentifier id)
                return id.Token.Text;

            if (expr is ExprMember member)
                return ToMemberPath(member.Target) + "." + member.Member.Text;

            throw new InvalidOperationException("Invalid member path.");
        }

        static string Unquote(string text)
        {
            if (text.Length >= 2 &&
                text[0] == text[^1] &&
                text[0] is '"' or '\'')
                return text[1..^1];

            return text;
        }

        static Token GetToken(Expr expr)
        {
            return expr switch
            {
                ExprIdentifier x => x.Token,
                ExprNumber x => x.Token,
                ExprString x => x.Token,
                ExprBoolean x => x.Token,
                ExprGroup x => x.Open,
                ExprUnary x => x.Op,
                ExprBinary x => x.Op,
                ExprTernary x => x.Question,
                ExprCall x => x.Open,
                ExprList x => x.Open,
                ExprRange x => GetToken(x.Start),
                ExprIndex x => x.Open,
                ExprMember x => x.Dot,
                ExprError x => x.Token,
                _ => default
            };
        }

        static Exception Runtime(Token token, string message)
            => new InvalidOperationException($"Runtime error at {token.Position}: {message}");
    }


}
