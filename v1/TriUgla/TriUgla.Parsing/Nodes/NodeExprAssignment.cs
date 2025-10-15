using TriUgla.Parsing.Compiling;
using TriUgla.Parsing.Exceptions;
using TriUgla.Parsing.Nodes.Literals;
using TriUgla.Parsing.Nodes.TupleOps;
using TriUgla.Parsing.Scanning;

namespace TriUgla.Parsing.Nodes
{
    public class NodeExprAssignment : NodeBase
    {
        public NodeExprAssignment(NodeBase id, Token op, NodeBase expression) : base(op)
        {
            Assignee = id;
            Expression = expression;
        }

        public NodeBase Assignee { get; }
        public ETokenType Operation => Token.type;
        public NodeBase Expression { get; }

        public override TuValue Evaluate(TuRuntime stack)
        {
            return Assignee switch
            {
                NodeExprValueAt valueAt => EvalAssignToElement(valueAt, stack),
                NodeExprIdentifier ident => EvalAssignToIdentifier(ident, stack),
                _ => throw new CompileTimeException(
                        "Left-hand side of assignment must be an identifier or an index into a tuple.",
                        Assignee.Token)
            };
        }

        TuValue EvalAssignToElement(NodeExprValueAt valueAt, TuRuntime stack)
        {
            TuValue currentElement = valueAt.Evaluate(stack); 
            EnsureTupleElementContext(valueAt);

            TuValue rhs = Expression.Evaluate(stack);
            EnsureNumeric(rhs, Expression.Token,
                "Cannot assign a non-numeric value to a tuple element.");

            valueAt.Tuple!.Values[valueAt.Index] = rhs.AsNumeric();
            return TuValue.Nothing;
        }

        static void EnsureTupleElementContext(NodeExprValueAt valueAt)
        {
            if (valueAt.Tuple is null)
                throw new RunTimeException("Internal error: missing tuple reference for element assignment.", valueAt.Token);
        }

        TuValue EvalAssignToIdentifier(NodeExprIdentifier id, TuRuntime stack)
        {
            id.DeclareIfMissing = Operation == ETokenType.Equal || id.DeclareIfMissing;

            TuValue current = id.Evaluate(stack); 
            TuValue rhs = Expression.Evaluate(stack);
            var variable = RequireVariable(stack, id);

            if (Operation == ETokenType.Equal)
                return AssignEqual(variable, rhs);

            EnsureInitializedForCompound(current, id);

            return rhs.type switch
            {
                EDataType.Tuple => AssignCompoundTuple(variable, current, rhs),
                EDataType.Numeric => AssignCompoundNumeric(variable, current, rhs),
                _ => throw new RunTimeException(
                        $"Unsupported right-hand side type '{rhs.type}' for operation '{Operation}'.",
                        Expression.Token)
            };
        }

        static Variable RequireVariable(TuRuntime stack, NodeExprIdentifier id)
        {
            var v = stack.Current.Get(id.Name);
            if (v is null)
                throw new RunTimeException($"Undefined variable '{id.Name}'.", id.Token);
            return v;
        }

        static TuValue AssignEqual(Variable variable, TuValue rhs)
        {
            variable.Value = rhs;
            return variable.Value;
        }

        void EnsureInitializedForCompound(TuValue current, NodeExprIdentifier id)
        {
            if (current.type == EDataType.Nothing)
                throw new RunTimeException(
                    $"Compound assignment '{Operation}' requires an initialized variable '{id.Name}'.",
                    Token);
        }

        TuValue AssignCompoundTuple(Variable variable, TuValue current, TuValue rhs)
        {
            if (current.type != EDataType.Tuple)
                throw new RunTimeException(
                    $"Invalid operation '{Operation}' between '{current.type}' and 'Tuple'.",
                    Token);

            if (Operation != ETokenType.PlusEqual)
                throw new RunTimeException(
                    $"Unsupported tuple compound operation '{Operation}'. Only '+=' is allowed for tuples.",
                    Token);

            var left = variable.Value.AsTuple()!;
            var right = rhs.AsTuple()!;

            var merged = ConcatTuples(left, right);
            variable.Value = new TuValue(new TuTuple(merged));
            return TuValue.Nothing;
        }

        static List<double> ConcatTuples(TuTuple left, TuTuple right)
        {
            var res = new List<double>(left.Values.Count + right.Values.Count);
            res.AddRange(left);
            res.AddRange(right);
            return res;
        }

        TuValue AssignCompoundNumeric(Variable variable, TuValue current, TuValue rhs)
        {
            if (current.type != EDataType.Numeric)
                throw new RunTimeException(
                    $"Invalid operation '{Operation}' between '{current.type}' and 'Numeric'.",
                    Token);

            double l = current.AsNumeric();
            double r = rhs.AsNumeric();

            double n = Operation switch
            {
                ETokenType.PlusEqual => l + r,
                ETokenType.MinusEqual => l - r,
                ETokenType.StarEqual => l * r,
                ETokenType.SlashEqual => r == 0
                    ? throw RunTimeException.DivisionByZero(Token)
                    : l / r,
                ETokenType.ModuloEqual => l % r,
                ETokenType.PowerEqual => Math.Pow(l, r),
                _ => throw new RunTimeException(
                        $"Unsupported compound operation '{Operation}' for numeric assignment.", Token)
            };

            variable.Value = new TuValue(n);
            return variable.Value;
        }

        static void EnsureNumeric(TuValue v, Token tok, string message)
        {
            if (v.type != EDataType.Numeric)
                throw new RunTimeException(message, tok);
        }
    }
}
