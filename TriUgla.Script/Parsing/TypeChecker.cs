using TriUgla.Script.Data;
using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public sealed class TypeChecker : INodeVisitor<ObjType>
    {
        readonly ScopeStack<ObjType> _scopes = new();
        readonly LoopTrack _loops = new();
        readonly Diagnostics _diagnostics;

        public TypeChecker(Diagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

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
            => node.Expression.Accept(this);

        public ObjType Visit(StmtAssign node)
        {
            ObjType value = node.Value.Accept(this);

            if (node.Target is ExprIdentifier id)
            {
                string name = id.Token.Text;

                if (_scopes.TryResolve(name, out var scope))
                {
                    ObjType current = scope[name];

                    if (!CanAssign(current, value))
                        Report(node.Op, $"Cannot assign {value} to '{name}' of type {current}.");

                    scope[name] = MergeAssignment(current, value);
                }
                else
                {
                    _scopes.Current.Declare(name, value);
                }

                return value;
            }

            if (node.Target is ExprIndex index)
                return CheckIndexAssignment(index, node.Op, value);

            if (node.Target is ExprMember)
                return value;

            Report(node.Op, "Left side of assignment must be identifier, index, or member.");
            return ObjType.Undefined;
        }

        public ObjType Visit(StmtIf node)
        {
            CheckCondition(node.Condition, "If");

            node.ThenBranch.Accept(this);

            foreach (StmtElseIf item in node.ElseIfs)
                item.Accept(this);

            node.ElseBranch?.Accept(this);

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtElseIf node)
        {
            CheckCondition(node.Condition, "ElseIf");
            node.Body.Accept(this);
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

        public ObjType Visit(StmtFor node)
        {
            ObjType iterable = node.Iterable.Accept(this);

            ObjType itemType = iterable.Kind switch
            {
                DataKind.Range => ObjType.Integer,
                DataKind.List => iterable.Element ?? ObjType.Undefined,
                _ => Error(node.Identifier, $"For expects Range or List, got {iterable}.")
            };

            using (_scopes.Guard())
            {
                _scopes.Current.Declare(node.Identifier.Text, itemType);

                _loops.Enter();
                node.Body.Accept(this);
                _loops.Exit();
            }

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtBreak node)
        {
            if (!_loops.IsInsideLoop)
                Report(default, "'Break' can only be used inside a loop.");

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtContinue node)
        {
            if (!_loops.IsInsideLoop)
                Report(default, "'Continue' can only be used inside a loop.");

            return ObjType.Undefined;
        }

        public ObjType Visit(StmtReturn node)
        {
            node.Value?.Accept(this);
            return ObjType.Undefined;
        }

        public ObjType Visit(StmtPoint node)
        {
            CheckInteger(node.Id, "Point id");
            CheckNumberList(node.Values, 4, "Point values must be {x, y, z, meshSize}.");
            return ObjType.Point;
        }

        public ObjType Visit(StmtLine node)
        {
            CheckInteger(node.Id, "Line id");
            CheckIntegerList(node.Values, 2, "Line values must be {startPointId, endPointId}.");
            return ObjType.Line;
        }

        public ObjType Visit(StmtCircle node)
        {
            CheckInteger(node.Id, "Circle id");
            CheckIntegerList(node.Values, 3, "Circle values must be {startPointId, centerPointId, endPointId}.");
            return ObjType.Circle;
        }

        public ObjType Visit(StmtEllipse node)
        {
            CheckInteger(node.Id, "Ellipse id");
            CheckIntegerList(node.Values, 4, "Ellipse values must be {startPointId, centerPointId, majorPointId, endPointId}.");
            return ObjType.Ellipse;
        }

        public ObjType Visit(StmtSpline node)
        {
            CheckInteger(node.Id, "Spline id");
            CheckIntegerList(node.Values, minCount: 2, "Spline needs at least 2 point ids.");
            return ObjType.Spline;
        }

        public ObjType Visit(StmtBSpline node)
        {
            CheckInteger(node.Id, "BSpline id");
            CheckIntegerList(node.Values, minCount: 2, "BSpline needs at least 2 point ids.");
            return ObjType.BSpline;
        }

        public ObjType Visit(StmtBezier node)
        {
            CheckInteger(node.Id, "Bezier id");
            CheckIntegerList(node.Values, minCount: 2, "Bezier needs at least 2 point ids.");
            return ObjType.Bezier;
        }

        public ObjType Visit(StmtCurveLoop node)
        {
            CheckInteger(node.Id, "Curve Loop id");
            CheckIntegerList(node.Values, minCount: 1, "Curve Loop needs at least one curve id.");
            return ObjType.CurveLoop;
        }

        public ObjType Visit(StmtPlaneSurface node)
        {
            CheckInteger(node.Id, "Plane Surface id");
            CheckIntegerList(node.Values, minCount: 1, "Plane Surface needs at least one curve loop id.");
            return ObjType.PlaneSurface;
        }

        public ObjType Visit(StmtPhysical node)
        {
            CheckInteger(node.Id, "Physical group id");
            CheckIntegerList(node.Values, minCount: 1, "Physical group needs at least one entity id.");
            node.Name?.Accept(this);
            return node.Kind switch
            {
                Keyword.Point => new ObjType(DataKind.PhysicalPoint),
                Keyword.Curve or Keyword.Line => new ObjType(DataKind.PhysicalCurve),
                Keyword.Surface => new ObjType(DataKind.PhysicalSurface),
                _ => ObjType.Undefined
            };
        }

        public ObjType Visit(StmtTransfiniteCurve node)
        {
            CheckIntegerList(node.Curves, minCount: 1, "Transfinite Curve needs at least one curve id.");
            CheckInteger(node.Divisions, "Transfinite Curve divisions");
            CheckNumber(node.Progression, "Transfinite Curve progression");
            return new ObjType(DataKind.TransfiniteCurve);
        }

        public ObjType Visit(StmtTransfiniteSurface node)
        {
            CheckIntegerList(node.Surfaces, minCount: 1, "Transfinite Surface needs at least one surface id.");

            if (node.Corners is not null)
                CheckIntegerList(node.Corners, minCount: 3, "Transfinite Surface corners need at least 3 point ids.");

            return new ObjType(DataKind.TransfiniteSurface);
        }

        public ObjType Visit(StmtRecombineSurface node)
        {
            CheckIntegerList(node.Surfaces, minCount: 1, "Recombine Surface needs at least one surface id.");
            return new ObjType(DataKind.RecombineSurface);
        }

        public ObjType Visit(StmtEmbed node)
        {
            CheckIntegerList(node.Entities, minCount: 1, "Embedded entity list cannot be empty.");
            CheckIntegerList(node.Containers, minCount: 1, "Embedding container list cannot be empty.");
            return new ObjType(DataKind.EmbedConstraint);
        }

        public ObjType Visit(StmtMeshOption node)
        {
            node.Value.Accept(this);
            return ObjType.MeshOption;
        }

        public ObjType Visit(StmtMeshCommand node)
        {
            foreach (Expr arg in node.Args)
                arg.Accept(this);

            return ObjType.MeshCommand;
        }

        public ObjType Visit(StmtError node)
        {
            Report(node.Token, node.Message);
            return ObjType.Undefined;
        }

        public ObjType Visit(ExprIdentifier node)
        {
            if (_scopes.TryGet(node.Token.Text, out ObjType type))
                return type;

            Report(node.Token, $"Unknown identifier '{node.Token.Text}'.");
            return ObjType.Undefined;
        }

        public ObjType Visit(ExprNumber node)
            => IsIntegerLiteral(node.Token) ? ObjType.Integer : ObjType.Double;

        public ObjType Visit(ExprString node)
            => ObjType.String;

        public ObjType Visit(ExprBoolean node)
            => ObjType.Boolean;

        public ObjType Visit(ExprGroup node)
            => node.Inner.Accept(this);

        public ObjType Visit(ExprUnary node)
        {
            ObjType right = node.Right.Accept(this);

            return node.Op.Operator switch
            {
                OperatorKind.Plus => RequireNumber(node.Op, right, "Unary '+'"),
                OperatorKind.Minus => RequireNumber(node.Op, right, "Unary '-'"),
                OperatorKind.Not => RequireBoolean(node.Op, right, "Unary '!'"),
                _ => Error(node.Op, $"Unsupported unary operator '{node.Op.Text}'.")
            };
        }

        public ObjType Visit(ExprBinary node)
        {
            ObjType left = node.Left.Accept(this);

            if (node.Op.Is(OperatorKind.And) || node.Op.Is(Keyword.And))
            {
                ObjType right = node.Right.Accept(this);
                if (left != ObjType.Boolean || right != ObjType.Boolean)
                    Report(node.Op, $"And expects Boolean operands, got {left} and {right}.");
                return ObjType.Boolean;
            }

            if (node.Op.Is(OperatorKind.Or) || node.Op.Is(Keyword.Or))
            {
                ObjType right = node.Right.Accept(this);
                if (left != ObjType.Boolean || right != ObjType.Boolean)
                    Report(node.Op, $"Or expects Boolean operands, got {left} and {right}.");
                return ObjType.Boolean;
            }

            ObjType rightType = node.Right.Accept(this);

            if (node.Op.Is(OperatorKind.Plus))
                return BinaryPlus(node.Op, left, rightType);

            if (node.Op.Is(OperatorKind.Minus) ||
                node.Op.Is(OperatorKind.Multiply) ||
                node.Op.Is(OperatorKind.Modulo) ||
                node.Op.Is(OperatorKind.Power))
                return BinaryNumber(node.Op, left, rightType);

            if (node.Op.Is(OperatorKind.Divide))
                return BinaryDivide(node.Op, node.Right, left, rightType);

            if (node.Op.Is(OperatorKind.Less) ||
                node.Op.Is(OperatorKind.LessEqual) ||
                node.Op.Is(OperatorKind.Greater) ||
                node.Op.Is(OperatorKind.GreaterEqual))
                return BinaryCompare(node.Op, left, rightType);

            if (node.Op.Is(OperatorKind.Equal) ||
                node.Op.Is(OperatorKind.NotEqual) ||
                node.Op.Is(Keyword.Is))
                return BinaryEquality(node.Op, left, rightType);

            return Error(node.Op, $"Unsupported binary operator '{node.Op.Text}'.");
        }

        public ObjType Visit(ExprTernary node)
        {
            CheckCondition(node.Condition, "Ternary condition");

            ObjType a = node.WhenTrue.Accept(this);
            ObjType b = node.WhenFalse.Accept(this);

            return CommonType(a, b);
        }

        public ObjType Visit(ExprCall node)
        {
            foreach (Expr arg in node.Args)
                arg.Accept(this);

            if (node.Target is not ExprIdentifier id)
                return ObjType.Undefined;

            if (!Builtins.TryGet(id.Token.Text, out BuiltinFunction fn))
                return ObjType.Undefined;

            int argc = node.Args.Count;

            if (argc < fn.MinArgs || argc > fn.MaxArgs)
            {
                Report(
                    id.Token,
                    $"Function '{id.Token.Text}' expects between {fn.MinArgs} and {fn.MaxArgs} arguments, got {argc}.");

                return ObjType.Undefined;
            }

            return fn.Kind switch
            {
                BuiltinFunctionKind.Print => ObjType.Undefined,
                BuiltinFunctionKind.Exit => ObjType.Undefined,

                BuiltinFunctionKind.Min => ObjType.Double,
                BuiltinFunctionKind.Max => ObjType.Double,
                BuiltinFunctionKind.Abs => ObjType.Double,

                BuiltinFunctionKind.Sin => ObjType.Double,
                BuiltinFunctionKind.Cos => ObjType.Double,
                BuiltinFunctionKind.Tan => ObjType.Double,

                BuiltinFunctionKind.Sqrt => ObjType.Double,
                BuiltinFunctionKind.Pow => ObjType.Double,

                BuiltinFunctionKind.Length => ObjType.Double,
                BuiltinFunctionKind.Dot => ObjType.Double,

                BuiltinFunctionKind.StrLen => ObjType.Integer,

                BuiltinFunctionKind.StrContains => ObjType.Boolean,
                BuiltinFunctionKind.StrStartsWith => ObjType.Boolean,
                BuiltinFunctionKind.StrEndsWith => ObjType.Boolean,

                BuiltinFunctionKind.StrLower => ObjType.String,
                BuiltinFunctionKind.StrUpper => ObjType.String,
                BuiltinFunctionKind.StrTrim => ObjType.String,
                BuiltinFunctionKind.StrReplace => ObjType.String,
                BuiltinFunctionKind.SubStr => ObjType.String,

                BuiltinFunctionKind.Transpose => ObjType.ListOf(
                    ObjType.ListOf(ObjType.Double)),

                BuiltinFunctionKind.Inverse => ObjType.ListOf(
                    ObjType.ListOf(ObjType.Double)),

                _ => ObjType.Undefined
            };
        }

        public ObjType Visit(ExprList node)
        {
            if (node.Values.Count == 0)
                return ObjType.ListOf(ObjType.Undefined);

            ObjType element = node.Values[0].Accept(this);

            for (int i = 1; i < node.Values.Count; i++)
                element = CommonType(element, node.Values[i].Accept(this));

            return ObjType.ListOf(element);
        }

        public ObjType Visit(ExprRange node)
        {
            CheckNumber(node.Start, "Range start");
            CheckNumber(node.End, "Range end");

            if (node.Step is not null)
                CheckNumber(node.Step, "Range step");

            return ObjType.Range;
        }

        public ObjType Visit(ExprIndex node)
        {
            ObjType target = node.Target.Accept(this);

            if (node.Index is null)
            {
                if (!target.IsList)
                    Report(node.Open, $"Empty index [] requires List, got {target}.");

                return target;
            }

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

        public ObjType Visit(ExprMember node)
        {
            node.Target.Accept(this);
            return ObjType.Undefined;
        }

        public ObjType Visit(ExprError node)
        {
            Report(node.Token, node.Message);
            return ObjType.Undefined;
        }

        ObjType CheckIndexAssignment(ExprIndex index, Token op, ObjType value)
        {
            ObjType target = index.Target.Accept(this);

            if (index.Index is null)
            {
                if (!target.IsList)
                    Report(index.Open, $"Empty index [] assignment requires List, got {target}.");

                if (op.Is(OperatorKind.Assign))
                    return value;

                if (op.Is(OperatorKind.PlusAssign))
                    return target;

                Report(op, "Only '=' and '+=' are valid for empty index assignment.");
                return ObjType.Undefined;
            }

            ObjType indexType = index.Index.Accept(this);

            if (indexType != ObjType.Integer)
                Report(index.Open, $"Index must be Integer, got {indexType}.");

            if (!target.IsList)
            {
                Report(index.Open, $"Type {target} is not index-assignable.");
                return ObjType.Undefined;
            }

            ObjType element = target.Element ?? ObjType.Undefined;

            if (!CanAssign(element, value))
                Report(op, $"Cannot assign {value} to list element of type {element}.");

            return value;
        }

        void CheckCondition(Expr condition, string owner)
        {
            ObjType type = condition.Accept(this);

            if (type != ObjType.Boolean)
                Report(GetToken(condition), $"{owner} must be Boolean, got {type}.");
        }

        void CheckInteger(Expr expr, string name)
        {
            ObjType type = expr.Accept(this);

            if (type != ObjType.Integer)
                Report(GetToken(expr), $"{name} must be Integer, got {type}.");
        }

        void CheckNumber(Expr expr, string name)
        {
            ObjType type = expr.Accept(this);

            if (!type.IsNumber)
                Report(GetToken(expr), $"{name} must be numeric, got {type}.");
        }

        void CheckIntegerList(Expr expr, int minCount, string message)
        {
            ObjType type = expr.Accept(this);

            if (!type.IsList || type.Element != ObjType.Integer)
                Report(GetToken(expr), $"{message} Expected List<Integer>, got {type}.");
        }

        void CheckNumberList(Expr expr, int exactCount, string message)
        {
            ObjType type = expr.Accept(this);

            if (!type.IsList || type.Element is null || !type.Element.IsNumber)
                Report(GetToken(expr), $"{message} Expected List<Number>, got {type}.");
        }

        ObjType BinaryPlus(Token op, ObjType left, ObjType right)
        {
            if (left == ObjType.String || right == ObjType.String)
                return ObjType.String;

            return BinaryNumber(op, left, right);
        }

        ObjType BinaryNumber(Token op, ObjType left, ObjType right)
        {
            if (!left.IsNumber || !right.IsNumber)
                return Error(op, $"Operator '{op.Text}' expects numbers, got {left} and {right}.");

            return PromoteNumber(left, right);
        }

        ObjType BinaryDivide(Token op, Expr rightExpr, ObjType left, ObjType right)
        {
            if (!left.IsNumber || !right.IsNumber)
                return Error(op, $"Operator '/' expects numbers, got {left} and {right}.");

            if (IsLiteralZero(rightExpr))
                Report(op, "Division by constant zero.");

            return ObjType.Double;
        }

        ObjType BinaryCompare(Token op, ObjType left, ObjType right)
        {
            if (!left.IsNumber || !right.IsNumber)
                Report(op, $"Comparison expects numbers, got {left} and {right}.");

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

        ObjType RequireNumber(Token token, ObjType type, string name)
        {
            if (!type.IsNumber)
                return Error(token, $"{name} expects number, got {type}.");

            return type;
        }

        ObjType RequireBoolean(Token token, ObjType type, string name)
        {
            if (type != ObjType.Boolean)
                Report(token, $"{name} expects Boolean, got {type}.");

            return ObjType.Boolean;
        }

        ObjType Error(Token token, string message)
        {
            Report(token, message);
            return ObjType.Undefined;
        }

        static ObjType PromoteNumber(ObjType a, ObjType b)
            => a == ObjType.Double || b == ObjType.Double ? ObjType.Double : ObjType.Integer;

        static ObjType CommonType(ObjType a, ObjType b)
        {
            if (a == b) return a;
            if (a.IsNumber && b.IsNumber) return PromoteNumber(a, b);

            if (a.IsList && b.IsList)
                return ObjType.ListOf(CommonType(a.Element ?? ObjType.Undefined, b.Element ?? ObjType.Undefined));

            return ObjType.Undefined;
        }

        static bool CanAssign(ObjType expected, ObjType actual)
        {
            if (expected == actual) return true;
            if (expected == ObjType.Double && actual == ObjType.Integer) return true;

            if (expected.IsList && actual.IsList)
                return CanAssign(expected.Element ?? ObjType.Undefined, actual.Element ?? ObjType.Undefined);

            return false;
        }

        static ObjType MergeAssignment(ObjType current, ObjType next)
            => CanAssign(current, next) ? current : CommonType(current, next);

        static bool IsLiteralZero(Expr expr)
            => expr is ExprNumber n && Math.Abs(n.Value) < 1e-12;

        static bool IsIntegerLiteral(Token token)
            => int.TryParse(token.Text, out _);

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

        void Report(Token token, string message)
        {
            _diagnostics.Error(message, token);
        }
    }
}
