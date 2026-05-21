using TriUgla.Script.Parsing.Nodes.Expressions;
using TriUgla.Script.Parsing.Nodes.Statements;

namespace TriUgla.Script.Parsing
{
    public interface INodeVisitor<T>
    {
        T Visit(StmtProg node);
        T Visit(StmtError node);
        T Visit(ExprError node);

        T Visit(StmtBlock node);
        T Visit(StmtExpr node);
        T Visit(StmtAssign node);

        T Visit(StmtIf node);
        T Visit(StmtElseIf node);
        T Visit(StmtFor node);
        T Visit(StmtWhile node);
        T Visit(StmtBreak node);
        T Visit(StmtContinue node);

        T Visit(ExprIdentifier node);
        T Visit(ExprNumber node);
        T Visit(ExprString node);
        T Visit(ExprBoolean node);
        T Visit(ExprUnary node);
        T Visit(ExprBinary node);
        T Visit(ExprGroup node);
        T Visit(ExprList node);
        T Visit(ExprRange node);
        T Visit(ExprIndex node);
    }
}
