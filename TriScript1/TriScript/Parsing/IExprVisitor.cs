using TriScript.Parsing.Nodes;

namespace TriScript.Parsing
{
    public interface IExprVisitor<T>
    {
        T Visit(ExprIdentifier node);
        T Visit(ExprAssignment node);
        T Visit(ExprBinary node);
        T Visit(ExprGroup node);
        T Visit(ExprNumeric node);
        T Visit(ExprUnaryPostfix node);
        T Visit(ExprUnaryPrefix node);
        T Visit(ExprWithUnit node);
    }
}
