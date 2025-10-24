using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;
using TriScript.UnitHandling;

namespace TriScript.Parsing
{
    public class VisitorEval : INodeVisitor<Value>
    {
        public VisitorEval(ScopeStack stack, Source source, Diagnostics diagnostics)
        {
            Scope = stack;
            Source = source;
        }

        public ScopeStack Scope { get; set; }
        public Source Source { get; set; }
        public Diagnostics Diagnostics { get; set; }
        public UnitRegistry Registry { get; set; }

        public bool Visit(ExprIdentifier node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprAssignment node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprBinary node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprGroup node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprNumeric node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprUnaryPostfix node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprUnaryPrefix node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(ExprWithUnit node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtBlock node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtPrint node, out Value result)
        {
            throw new NotImplementedException();
        }

        public bool Visit(StmtProgram node, out Value result)
        {
            throw new NotImplementedException();
        }
    }
}
