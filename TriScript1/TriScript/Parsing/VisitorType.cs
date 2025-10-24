using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public sealed class VisitorType : VisitorBase<EDataType>
    {
        public VisitorType(ScopeStack scope, Source source, Diagnostics diagnostics)
            : base(scope, source, diagnostics) { }

        public override bool Visit(StmtProgram node, out EDataType result)
        {
            using (WithScope())
            {
                node.Block.Accept(this, out _);
            }
            result = EDataType.None;
            return true;
        }

        public override bool Visit(StmtBlock node, out EDataType result)
        {
            using (WithScope())
            {
                bool ok = true;
                foreach (var stmt in node.Statements)
                    ok &= stmt.Accept(this, out _);
                result = EDataType.None;
                return ok;
            }
        }

        public override bool Visit(StmtExpr node, out EDataType result)
            => node.Inner.Accept(this, out result);

        public override bool Visit(StmtPrint node, out EDataType result)
        {
            result = EDataType.None;
            return true;
        }

        public override bool Visit(ExprAssignment node, out EDataType result)
        {
            result = EDataType.None;

            string name = SymbolName(node.Assignee);

            if (!node.Value.Accept(this, out var typeAssign))
                return false;

            // Resolve existing variable or declare in current scope
            Variable variable;
            if (!TryResolve(name, out variable))
                variable = DeclareHere(name);

            var current = variable.Value.type;

            if (current == EDataType.None)
            {
                // First assignment sets the variable type
                variable.Value = new Value(typeAssign);
                result = typeAssign;
                return true;
            }

            if (!TypesCompatible(current, typeAssign))
            {
                Report(
                    $"Cannot assign value of type '{typeAssign}' to variable '{name}' of type '{current}'.",
                    node.Assignee.Token, node.Value.Token);
                return false;
            }

            result = current;
            return true;
        }

        public override bool Visit(ExprBinary node, out EDataType result)
        {
            result = EDataType.None;

            if (!node.Left.Accept(this, out var l) || !node.Right.Accept(this, out var r))
                return false;

            result = Output(l, node.Operator.type, r);
            if (result != EDataType.None)
                return true;

            Report(
                $"Operator '{Source.GetString(node.Operator.span)}' cannot be applied to operands of types '{l}' and '{r}'.",
                node.Operator);
            return false;
        }

        public override bool Visit(ExprGroup node, out EDataType result)
            => node.Inner.Accept(this, out result);

        public override bool Visit(ExprLiteralInteger node, out EDataType result)
        { result = EDataType.Integer; return true; }

        public override bool Visit(ExprLiteralReal node, out EDataType result)
        { result = EDataType.Real; return true; }

        public override bool Visit(ExprLiteralString node, out EDataType result)
        { result = EDataType.String; return true; }

        public override bool Visit(ExprLiteralSymbol node, out EDataType result)
        {
            if (TryResolve(node.ToString(), out var v))
            {
                result = v.Value.type;
                if (result == EDataType.None)
                {
                    Report($"Variable '{node}' is declared but has no type yet.", node.Token);
                    return false;
                }
                return true;
            }
            Report($"Undeclared variable '{node}'.", node.Token);
            result = EDataType.None;
            return false;
        }

        public override bool Visit(ExprUnaryPrefix node, out EDataType result)
        {
            if (!node.Right.Accept(this, out result))
                return false;

            var op = node.Operator.type;

            if (op == ETokenType.Not)
            {
                if (!result.IsNumeric())
                {
                    Report($"Logical 'not' requires numeric/bool, got '{result}'.", node.Operator);
                    return false;
                }
                result = EDataType.Integer; // your “bool as int” model
                return true;
            }

            if (op == ETokenType.Plus || op == ETokenType.Minus)
            {
                if (!result.IsNumeric())
                {
                    Report($"Unary '{Source.GetString(node.Operator.span)}' requires numeric operand, got '{result}'.", node.Operator);
                    return false;
                }
                return true; // type unchanged (int stays int, real stays real)
            }

            if (op == ETokenType.PlusPlus || op == ETokenType.MinusMinus)
            {
                // Must be lvalue & numeric
                if (node.Right is not ExprLiteralSymbol sym)
                {
                    Report($"Operator '{Source.GetString(node.Operator.span)}' requires a variable.", node.Operator);
                    return false;
                }
                if (!TryResolve(sym.ToString(), out var v))
                {
                    Report($"Undeclared variable '{sym}'.", node.Operator);
                    return false;
                }
                if (!v.Value.type.IsNumeric())
                {
                    Report($"Operator '{Source.GetString(node.Operator.span)}' requires numeric variable, got '{v.Value.type}'.", node.Operator);
                    return false;
                }
                result = v.Value.type; // prefix returns updated value; type is same
                return true;
            }

            Report($"Unsupported unary operator '{Source.GetString(node.Operator.span)}'.", node.Operator);
            return false;
        }

        public override bool Visit(ExprUnaryPostfix node, out EDataType result)
        {
            // x++ / x-- : must be variable and numeric
            result = EDataType.None;

            if (node.Left is not ExprLiteralSymbol sym)
            {
                Report("Postfix operator applies only to variables.", node.Operator);
                return false;
            }

            if (!TryResolve(sym.ToString(), out var v))
            {
                Report($"Undeclared variable '{sym}'.", node.Operator);
                return false;
            }

            if (!v.Value.type.IsNumeric())
            {
                Report($"Operator '{Source.GetString(node.Operator.span)}' requires numeric variable, got '{v.Value.type}'.", node.Operator);
                return false;
            }

            result = v.Value.type; // postfix result type = variable type
            return true;
        }

        public override bool Visit(ExprWithUnit node, out EDataType result)
        {
            // If units imply numeric, enforce numeric inside
            if (!node.Inner.Accept(this, out var inner))
            {
                result = EDataType.None;
                return false;
            }
            if (!inner.IsNumeric())
            {
                Report($"Unit expression requires numeric value, got '{inner}'.", node.Token);
                result = EDataType.None;
                return false;
            }
            result = EDataType.Real; // usually treat unit-bearing values as Real
            return true;
        }

        // ---------- type rules ----------
        public static bool TypesCompatible(EDataType left, EDataType right)
            => left == right || (left == EDataType.Real && right == EDataType.Integer);

        public static EDataType Output(EDataType l, ETokenType op, EDataType r)
        {
            // String concatenation
            if (op == ETokenType.Plus && (l == EDataType.String || r == EDataType.String))
                return EDataType.String;

            if (l.IsNumeric() && r.IsNumeric())
            {
                switch (op.Type())
                {
                    case EOperatorType.Arythmetic:
                        return (l == EDataType.Real || r == EDataType.Real) ? EDataType.Real : EDataType.Integer;

                    case EOperatorType.Comparison:
                    case EOperatorType.Equality:
                    case EOperatorType.Boolean:
                        return EDataType.Integer; // your boolean
                }
            }

            return EDataType.None;
        }
    }

}
