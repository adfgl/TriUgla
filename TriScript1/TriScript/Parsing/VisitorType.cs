using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public class VisitorType : INodeVisitor<EDataType>
    {
        const EDataType INT = EDataType.Integer;
        const EDataType REAL = EDataType.Real;
        const EDataType NONE = EDataType.None;

        const ETokenType PLUS = ETokenType.Plus;
        const ETokenType MINUS = ETokenType.Minus;
        const ETokenType MULT = ETokenType.Star;
        const ETokenType DIV = ETokenType.Slash;
        const ETokenType POW = ETokenType.Caret;

        const ETokenType EQL = ETokenType.Equal;
        const ETokenType NEQL = ETokenType.NotEqual;

        const ETokenType LESS = ETokenType.Less;
        const ETokenType GRTR = ETokenType.Greater;
        const ETokenType LESS_EQL = ETokenType.LessEqual;
        const ETokenType GRTR_EQL = ETokenType.GreaterEqual;

        const ETokenType OR = ETokenType.Or;
        const ETokenType AND = ETokenType.And;
        const ETokenType IS = ETokenType.Is;

        public VisitorType(ScopeStack stack, Source source, Diagnostics diagnostics)
        {
            Scope = stack;
            Source = source;
            Diagnostics = diagnostics;
        }

        public ScopeStack Scope { get; set; }
        public Source Source { get; set; }
        public Diagnostics Diagnostics { get; set; }

        public bool Visit(ExprIdentifier node, out EDataType result)
        {
            string name = Source.GetString(node.Id.span);
            if (Scope.Current.TryGet(name, out Variable var))
            {
                result = var.Value.type;
                return true;
            }

            result = EDataType.None;
            return false;
        }

        public static bool TypesCompatible(EDataType left, EDataType right)
        {
            return true;
        }

        public bool Visit(ExprAssignment node, out EDataType result)
        {
            result = EDataType.None;

            string name = Source.GetString(node.Assignee.span);
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
                    Diagnostics.Report(Source, ESeverity.Error, msg, node.Assignee.position, TextSpan.Sum(node.Assignee.span, node.Value.Token.span));
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

            ETokenType op = node.Operator.type;
            if (_returnTypes.TryGetValue((l, op, r), out result))
            {
                return true;
            }
            throw new Exception($"Invalid {l}{op}{r}");
        }

        public bool Visit(ExprGroup node, out EDataType result)
        {
            return node.Inner.Accept(this, out result);
        }

        public bool Visit(ExprNumeric node, out EDataType result)
        {
            result = node.Value.type;
            return true;
        }

        public bool Visit(ExprUnaryPostfix node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprUnaryPrefix node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprWithUnit node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtBlock node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtPrint node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtProgram node, out EDataType result)
        {
            throw new NotImplementedException();
        }

        Dictionary<(EDataType, ETokenType, EDataType), EDataType> _returnTypes = new Dictionary<(EDataType, ETokenType, EDataType), EDataType>()
        {
            // +
            { (INT,  PLUS, INT), INT },
            { (INT,  PLUS, REAL), NONE },
            { (REAL, PLUS, INT), REAL },
            { (REAL, PLUS, REAL), REAL },

            // -
            { (INT,  MINUS, INT), INT },
            { (INT,  MINUS, REAL), NONE },
            { (REAL, MINUS, INT), REAL },
            { (REAL, MINUS, REAL), REAL },

            // *
            { (INT,  MULT, INT), INT },
            { (INT,  MULT, REAL), NONE },
            { (REAL, MULT, INT), REAL },
            { (REAL, MULT, REAL), REAL },

            // /
            { (INT,  DIV, INT), INT },
            { (INT,  DIV, REAL), NONE },
            { (REAL, DIV, INT), REAL },
            { (REAL, DIV, REAL), REAL },

            // ^
            { (INT,  POW, INT), INT },
            { (INT,  POW, REAL), NONE },
            { (REAL, POW, INT), REAL },
            { (REAL, POW, REAL), REAL },

            // ==
            { (INT,  EQL, INT), INT },
            { (INT,  EQL, REAL), INT },
            { (REAL, EQL, INT), INT },
            { (REAL, EQL, REAL), INT },

            // !=
            { (INT,  NEQL, INT), INT },
            { (INT,  NEQL, REAL), INT },
            { (REAL, NEQL, INT), INT },
            { (REAL, NEQL, REAL), INT },

            // <
            { (INT,  LESS, INT), INT },
            { (INT,  LESS, REAL), INT },
            { (REAL, LESS, INT), INT },
            { (REAL, LESS, REAL), INT },

            // >
            { (INT,  GRTR, INT), INT },
            { (INT,  GRTR, REAL), INT },
            { (REAL, GRTR, INT), INT },
            { (REAL, GRTR, REAL), INT },

            // <=
            { (INT,  LESS_EQL, INT), INT },
            { (INT,  LESS_EQL, REAL), INT },
            { (REAL, LESS_EQL, INT), INT },
            { (REAL, LESS_EQL, REAL), INT },

            // >=
            { (INT,  GRTR_EQL, INT), INT },
            { (INT,  GRTR_EQL, REAL), INT },
            { (REAL, GRTR_EQL, INT), INT },
            { (REAL, GRTR_EQL, REAL), INT },

            // or
            { (INT,  OR, INT), INT },
            { (INT,  OR, REAL), INT },
            { (REAL, OR, INT), INT },
            { (REAL, OR, REAL), INT },

            // and
            { (INT,  AND, INT), INT },
            { (INT,  AND, REAL), INT },
            { (REAL, AND, INT), INT },
            { (REAL, AND, REAL), INT },

            // is
            { (INT,  IS, INT), INT },
            { (INT,  IS, REAL), INT },
            { (REAL, IS, INT), INT },
            { (REAL, IS, REAL), INT },
        };

    }
}
