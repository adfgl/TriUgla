using TriScript.Data;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace TriScript.Parsing
{
    public interface INodeVisitor<T>
    {
        Source Source { get; }
        ScopeStack Scope { get; }
        Diagnostics Diagnostics { get; }

        bool Visit(ExprIdentifier node, out T? result);
        bool Visit(ExprAssignment node, out T? result);
        bool Visit(ExprBinary node, out T? result);
        bool Visit(ExprGroup node, out T? result);
        bool Visit(ExprLiteral node, out T? result);
        bool Visit(ExprUnaryPostfix node, out T? result);
        bool Visit(ExprUnaryPrefix node, out T? result);
        bool Visit(ExprWithUnit node, out T? result);

        bool Visit(StmtBlock node, out T? result);
        bool Visit(StmtPrint node, out T? result);
        bool Visit(StmtProgram node, out T? result);
    }
}
