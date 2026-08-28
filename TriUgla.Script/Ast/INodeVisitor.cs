namespace TriUgla.Script;

public interface INodeVisitor<out TResult>
{
    TResult VisitCompilationUnit(CompilationUnit node);
    TResult VisitNameExpression(NameExpr node);
    TResult VisitLiteralExpression(LiteralExpr node);
    TResult VisitErrorExpression(ErrorExpr node);
    TResult VisitUnaryExpression(UnaryExpr node);
    TResult VisitBinaryExpression(BinaryExpr node);
    TResult VisitGroupExpression(GroupExpr node);
    TResult VisitCallExpression(CallExpr node);
    TResult VisitListExpression(ListExpr node);
    TResult VisitExpressionStatement(ExpressionStmt node);
    TResult VisitAssignmentStatement(AssignmentStmt node);
    TResult VisitBlockStatement(BlockStmt node);
}
