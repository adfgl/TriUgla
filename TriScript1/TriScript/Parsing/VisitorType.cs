using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public class VisitorType : INodeVisitor<EDataType>
    {
        public VisitorType(ScopeStack stack, Source source, Diagnostics diagnostics)
        {
            Scope = stack;
            Source = source;
            Diagnostics = diagnostics;
        }

        public ScopeStack Scope { get; }
        public Source Source { get; }
        public Diagnostics Diagnostics { get; }

        public static bool TypesCompatible(EDataType left, EDataType right)
        {
            if (left == right) return true;
            if (left == EDataType.Real && right == EDataType.Integer)
            {
                return true;
            }
            return false;
        }

        public bool Visit(ExprAssignment node, out EDataType result)
        {
            result = EDataType.None;

            string name = node.Assignee.ToString();
            if (Scope.Current.TryGet(name, out Variable var))
            {
                EDataType typeCurrent = var.Value.type;
                if (!node.Value.Accept(this, out EDataType typeAssign))
                {
                    // failed to evaluate, do not report errors
                    return false;
                }

                if (typeCurrent == EDataType.None)
                {
                    result = typeAssign;
                    return true;
                }

                if (!TypesCompatible(typeCurrent, typeAssign))
                {
                    string msg = $"Cannot assign value of type '{typeAssign}' to variable '{name}' of type '{typeCurrent}'.";
                    Diagnostics.Report(Source, ESeverity.Error, msg, node.Assignee.Token.position, TextSpan.Sum(node.Assignee.Token.span, node.Value.Token.span));
                    return false;
                }

                result = typeCurrent;
                return true;
            }
            return false;
        }

        public bool Visit(ExprBinary node, out EDataType result)
        {
            result = EDataType.None;
            if (!node.Left.Accept(this, out EDataType l) ||
                !node.Right.Accept(this, out EDataType r))
            {
                return false;
            }

            if (l is EDataType.None || r is EDataType.None)
            {
                result = EDataType.None;
                return false;
            }

            result = Output(l, node.Operator.type, r);
            if (result != EDataType.None)
            {
                return true;
            }
            return false;
        }

        public bool Visit(ExprGroup node, out EDataType result)
        {
            return node.Inner.Accept(this, out result);
        }

        public bool Visit(ExprLiteralInteger node, out EDataType result)
        {
            result = EDataType.None;
            return true;
        }

        public bool Visit(ExprLiteralReal node, out EDataType result)
        {
            result = EDataType.None;
            return true;
        }

        public bool Visit(ExprLiteralString node, out EDataType result)
        {
            result = EDataType.None;
            return true;
        }

        public bool Visit(ExprLiteralSymbol node, out EDataType result)
        {
            if (Scope.Current.TryGet(node.ToString(), out Variable var))
            {
                result = var.Value.type;
                return true;
            }
            result = EDataType.None;
            return false;
        }

        public bool Visit(ExprUnaryPostfix node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprUnaryPrefix node, out EDataType result)
        {
            return node.Right.Accept(this, out result);
        }

        public bool Visit(ExprWithUnit node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtBlock node, out EDataType result)
        {
            foreach (var stmt in node.Statements)
            {
                if (!stmt.Accept(this, out _))
                {
                    break;
                }
            }

            result = EDataType.None;
            return true;
        }

        public bool Visit(StmtPrint node, out EDataType result)
        {
            result = EDataType.None;
            return true;
        }

        public bool Visit(StmtProgram node, out EDataType result)
        {
            result = EDataType.None;
            return true;
        }

        public static EDataType Output(EDataType l, ETokenType op, EDataType r)
        {
            if (op == ETokenType.Plus && (l == EDataType.String || r == EDataType.String))
            {
                return EDataType.String;
            }

            if (l.IsNumeric() && r.IsNumeric())
            {
                switch (op.Type())
                {
                    case EOperatorType.Arythmetic:
                        if (l == EDataType.Real || r == EDataType.Real)
                        {
                            return EDataType.Real;
                        }
                        return EDataType.Integer;

                    case EOperatorType.Comparison:
                    case EOperatorType.Equality:
                    case EOperatorType.Boolean:
                        return EDataType.Integer;

                    default:
                        throw new Exception();
                }
            }

            return EDataType.None;
        }

        public bool Visit(StmtExpr node, out EDataType result)
        {
            return node.Inner.Accept(this, out result);
        }

      
    }
}
