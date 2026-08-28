namespace TriUgla.Script;

public abstract record AstNode(TextSpan Span)
{
    public abstract TResult Accept<TResult>(INodeVisitor<TResult> visitor);
}

public abstract record Stmt(TextSpan Span) : AstNode(Span);

public abstract record Expr(TextSpan Span) : AstNode(Span);

public sealed record CompilationUnit(
    IReadOnlyList<Stmt> Statements,
    Token EndOfFile)
    : AstNode(GetSpan(Statements, EndOfFile))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitCompilationUnit(this);

    static TextSpan GetSpan(IReadOnlyList<Stmt> statements, Token endOfFile)
        => statements.Count == 0
            ? endOfFile.Span
            : TextSpan.FromBounds(statements[0].Span, endOfFile.Span);
}

public sealed record NameExpr(Token Name) : Expr(Name.Span)
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitNameExpression(this);
}

public sealed record LiteralExpr(Token Token, Value Value) : Expr(Token.Span)
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitLiteralExpression(this);
}

public sealed record ErrorExpr(Token Token) : Expr(Token.Span)
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitErrorExpression(this);
}

public sealed record UnaryExpr(Token Operator, Expr Operand)
    : Expr(TextSpan.FromBounds(Operator.Span, Operand.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitUnaryExpression(this);
}

public sealed record BinaryExpr(Expr Left, Token Operator, Expr Right)
    : Expr(TextSpan.FromBounds(Left.Span, Right.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitBinaryExpression(this);
}

public sealed record GroupExpr(
    Token LeftParenthesis,
    Expr Expression,
    Token RightParenthesis)
    : Expr(TextSpan.FromBounds(LeftParenthesis.Span, RightParenthesis.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitGroupExpression(this);
}

public sealed record CallExpr(
    Expr Callee,
    Token LeftParenthesis,
    IReadOnlyList<Expr> Arguments,
    Token RightParenthesis)
    : Expr(TextSpan.FromBounds(Callee.Span, RightParenthesis.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitCallExpression(this);
}

public sealed record ListExpr(
    Token LeftBrace,
    IReadOnlyList<Expr> Items,
    Token RightBrace)
    : Expr(TextSpan.FromBounds(LeftBrace.Span, RightBrace.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitListExpression(this);
}

public sealed record ExpressionStmt(Expr Expression, Token Semicolon)
    : Stmt(TextSpan.FromBounds(Expression.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitExpressionStatement(this);
}

public sealed record AssignmentStmt(
    Expr Target,
    Token EqualsToken,
    Expr Value,
    Token Semicolon)
    : Stmt(TextSpan.FromBounds(Target.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitAssignmentStatement(this);
}

public sealed record BlockStmt(
    Token LeftBrace,
    IReadOnlyList<Stmt> Statements,
    Token RightBrace)
    : Stmt(TextSpan.FromBounds(LeftBrace.Span, RightBrace.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitBlockStatement(this);
}
