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

public sealed record IndexExpr(
    Expr Target,
    Token LeftBracket,
    Expr Index,
    Token RightBracket)
    : Expr(TextSpan.FromBounds(Target.Span, RightBracket.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitIndexExpression(this);
}

public sealed record MemberAccessExpr(
    Expr Target,
    Token Dot,
    Token Member)
    : Expr(TextSpan.FromBounds(Target.Span, Member.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitMemberAccessExpression(this);
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

public sealed record ConditionalBranch(
    Token Keyword,
    Expr? Condition,
    IReadOnlyList<Stmt> Statements)
{
    public TextSpan Span => Statements.Count > 0
        ? TextSpan.FromBounds(Keyword.Span, Statements[^1].Span)
        : Condition is null
            ? Keyword.Span
            : TextSpan.FromBounds(Keyword.Span, Condition.Span);
}

public sealed record IfStmt(
    IReadOnlyList<ConditionalBranch> Branches,
    Token EndIfKeyword)
    : Stmt(GetSpan(Branches, EndIfKeyword))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitIfStatement(this);

    static TextSpan GetSpan(IReadOnlyList<ConditionalBranch> branches, Token endIfKeyword)
        => branches.Count == 0
            ? endIfKeyword.Span
            : TextSpan.FromBounds(branches[0].Keyword.Span, endIfKeyword.Span);
}

public sealed record ForStmt(
    Token ForKeyword,
    Token? Iterator,
    Expr? Start,
    Expr? End,
    Expr? Step,
    IReadOnlyList<Expr>? Items,
    IReadOnlyList<Stmt> Statements,
    Token EndForKeyword)
    : Stmt(TextSpan.FromBounds(ForKeyword.Span, EndForKeyword.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitForStatement(this);
}

public sealed record TransfiniteCurveStmt(
    Token TransfiniteKeyword,
    Token CurveKeyword,
    Token LeftBrace,
    IReadOnlyList<Expr> Curves,
    Token RightBrace,
    Token EqualsToken,
    Expr NodeCount,
    Token? UsingKeyword,
    Token? DistributionKeyword,
    Expr? Coefficient,
    Token Semicolon)
    : Stmt(TextSpan.FromBounds(TransfiniteKeyword.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitTransfiniteCurveStatement(this);
}

public sealed record CurveLoopStmt(
    Token CurveOrLineKeyword,
    Token LoopKeyword,
    Token LeftParenthesis,
    Expr Tag,
    Token RightParenthesis,
    Token EqualsToken,
    ListExpr Curves,
    Token Semicolon)
    : Stmt(TextSpan.FromBounds(CurveOrLineKeyword.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitCurveLoopStatement(this);
}

public sealed record PlaneSurfaceStmt(
    Token PlaneKeyword,
    Token SurfaceKeyword,
    Token LeftParenthesis,
    Expr Tag,
    Token RightParenthesis,
    Token EqualsToken,
    ListExpr CurveLoops,
    Token Semicolon)
    : Stmt(TextSpan.FromBounds(PlaneKeyword.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitPlaneSurfaceStatement(this);
}

public sealed record CurvesInSurfaceStmt(
    Token CurveOrLineKeyword,
    ListExpr Curves,
    Token InKeyword,
    Token SurfaceKeyword,
    ListExpr Surfaces,
    Token Semicolon)
    : Stmt(TextSpan.FromBounds(CurveOrLineKeyword.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitCurvesInSurfaceStatement(this);
}

public sealed record MeshCommandStmt(
    Token Command,
    Token? MeshKeyword,
    Expr? Dimension,
    Token Semicolon)
    : Stmt(TextSpan.FromBounds(Command.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitMeshCommandStatement(this);
}

public sealed record PhysicalPointStmt(
    Token PhysicalKeyword,
    Token PointKeyword,
    Token LeftParenthesis,
    Expr Name,
    Token RightParenthesis,
    Token EqualsToken,
    ListExpr Points,
    Token Semicolon)
    : Stmt(TextSpan.FromBounds(PhysicalKeyword.Span, Semicolon.Span))
{
    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor)
        => visitor.VisitPhysicalPointStatement(this);
}
