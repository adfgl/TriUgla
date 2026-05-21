using TriUgla.Script.Scanning;

namespace TriUgla.Script.Parsing
{
    public interface INodeVisitor<T>
    {
        // root
        T Visit(StmtProg node);

        // blocks
        T Visit(StmtBlock node);

        // control flow
        T Visit(StmtIf node);
        T Visit(StmtElseIf node);
        T Visit(StmtWhile node);
        T Visit(StmtFor node);

        T Visit(StmtBreak node);
        T Visit(StmtContinue node);
        T Visit(StmtReturn node);

        // generic statements
        T Visit(StmtExpr node);
        T Visit(StmtAssign node);

        // geometry
        T Visit(StmtPoint node);

        T Visit(StmtLine node);
        T Visit(StmtCircle node);
        T Visit(StmtEllipse node);

        T Visit(StmtSpline node);
        T Visit(StmtBSpline node);
        T Visit(StmtBezier node);

        T Visit(StmtCurveLoop node);
        T Visit(StmtPlaneSurface node);

        // physical groups
        T Visit(StmtPhysical node);

        // transfinite
        T Visit(StmtTransfiniteCurve node);
        T Visit(StmtTransfiniteSurface node);

        // recombination
        T Visit(StmtRecombineSurface node);

        // embedding
        T Visit(StmtEmbed node);

        // mesh
        T Visit(StmtMeshOption node);
        T Visit(StmtMeshCommand node);

        // diagnostics
        T Visit(StmtError node);

        // expressions
        T Visit(ExprIdentifier node);

        T Visit(ExprNumber node);
        T Visit(ExprString node);
        T Visit(ExprBoolean node);

        T Visit(ExprGroup node);

        T Visit(ExprUnary node);
        T Visit(ExprBinary node);
        T Visit(ExprTernary node);

        T Visit(ExprCall node);

        T Visit(ExprList node);
        T Visit(ExprRange node);

        T Visit(ExprIndex node);

        T Visit(ExprMember node);

        T Visit(ExprError node);
    }

    public abstract class Node
    {
        public abstract T Accept<T>(INodeVisitor<T> visitor);
    }

    public abstract class Stmt : Node { }

    public abstract class Expr : Node { }

    #region statements
    public sealed class StmtProg(IReadOnlyList<Stmt> statements) : Stmt
    {
        public IReadOnlyList<Stmt> Statements { get; } = statements;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtBlock(IReadOnlyList<Stmt> statements) : Stmt
    {
        public IReadOnlyList<Stmt> Statements { get; } = statements;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtIf(
        Expr condition,
        Stmt thenBranch,
        IReadOnlyList<StmtElseIf> elseIfs,
        Stmt? elseBranch) : Stmt
    {
        public Expr Condition { get; } = condition;
        public Stmt ThenBranch { get; } = thenBranch;

        public IReadOnlyList<StmtElseIf> ElseIfs { get; } = elseIfs;

        public Stmt? ElseBranch { get; } = elseBranch;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtElseIf(Expr condition, Stmt body) : Stmt
    {
        public Expr Condition { get; } = condition;
        public Stmt Body { get; } = body;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtWhile(Expr condition, Stmt body) : Stmt
    {
        public Expr Condition { get; } = condition;
        public Stmt Body { get; } = body;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtFor(Token identifier, Expr iterable, Stmt body) : Stmt
    {
        public Token Identifier { get; } = identifier;

        public Expr Iterable { get; } = iterable;

        public Stmt Body { get; } = body;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtBreak(Token token) : Stmt
    {
        public Token Token { get; } = token;
        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtContinue(Token token) : Stmt
    {
        public Token Token { get; } = token;
        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtReturn(Expr? value) : Stmt
    {
        public Expr? Value { get; } = value;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtExpr(Expr expression) : Stmt
    {
        public Expr Expression { get; } = expression;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtAssign(Expr target, Token op, Expr value) : Stmt
    {
        public Expr Target { get; } = target;

        public Token Op { get; } = op;

        public Expr Value { get; } = value;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtPoint(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtLine(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtCircle(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtEllipse(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtSpline(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtBSpline(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtBezier(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtCurveLoop(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtPlaneSurface(Expr id, Expr values) : Stmt
    {
        public Expr Id { get; } = id;
        public Expr Values { get; } = values;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtPhysical(
        Keyword kind,
        Expr id,
        Expr values,
        Expr? name) : Stmt
    {
        public Keyword Kind { get; } = kind;

        public Expr Id { get; } = id;

        public Expr Values { get; } = values;

        public Expr? Name { get; } = name;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtTransfiniteCurve(
        Expr curves,
        Expr divisions,
        Expr progression) : Stmt
    {
        public Expr Curves { get; } = curves;

        public Expr Divisions { get; } = divisions;

        public Expr Progression { get; } = progression;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtTransfiniteSurface(
        Expr surfaces,
        Expr? corners) : Stmt
    {
        public Expr Surfaces { get; } = surfaces;

        public Expr? Corners { get; } = corners;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtRecombineSurface(Expr surfaces) : Stmt
    {
        public Expr Surfaces { get; } = surfaces;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtEmbed(
        Keyword entity,
        Expr entities,
        Keyword container,
        Expr containers) : Stmt
    {
        public Keyword Entity { get; } = entity;

        public Expr Entities { get; } = entities;

        public Keyword Container { get; } = container;

        public Expr Containers { get; } = containers;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtMeshOption(
        IReadOnlyList<Token> path,
        Expr value) : Stmt
    {
        public IReadOnlyList<Token> Path { get; } = path;

        public Expr Value { get; } = value;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtMeshCommand(
        IReadOnlyList<Token> path,
        IReadOnlyList<Expr> args) : Stmt
    {
        public IReadOnlyList<Token> Path { get; } = path;

        public IReadOnlyList<Expr> Args { get; } = args;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class StmtError(Token token, string message) : Stmt
    {
        public Token Token { get; } = token;

        public string Message { get; } = message;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }
    #endregion statements

    #region expressions
    public sealed class ExprIdentifier(Token token) : Expr
    {
        public Token Token { get; } = token;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprNumber(Token token, double value) : Expr
    {
        public Token Token { get; } = token;

        public double Value { get; } = value;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprString(Token token, string value) : Expr
    {
        public Token Token { get; } = token;

        public string Value { get; } = value;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprBoolean(Token token, bool value) : Expr
    {
        public Token Token { get; } = token;

        public bool Value { get; } = value;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprGroup(Token open, Expr inner, Token close) : Expr
    {
        public Token Open { get; } = open;

        public Expr Inner { get; } = inner;

        public Token Close { get; } = close;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprUnary(Token op, Expr right) : Expr
    {
        public Token Op { get; } = op;

        public Expr Right { get; } = right;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprBinary(Expr left, Token op, Expr right) : Expr
    {
        public Expr Left { get; } = left;

        public Token Op { get; } = op;

        public Expr Right { get; } = right;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprTernary(
        Expr condition,
        Token question,
        Expr whenTrue,
        Token colon,
        Expr whenFalse) : Expr
    {
        public Expr Condition { get; } = condition;

        public Token Question { get; } = question;

        public Expr WhenTrue { get; } = whenTrue;

        public Token Colon { get; } = colon;

        public Expr WhenFalse { get; } = whenFalse;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprCall(
        Expr target,
        Token open,
        IReadOnlyList<Expr> args,
        Token close) : Expr
    {
        public Expr Target { get; } = target;

        public Token Open { get; } = open;

        public IReadOnlyList<Expr> Args { get; } = args;

        public Token Close { get; } = close;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprList(
        Token open,
        IReadOnlyList<Expr> values,
        Token close) : Expr
    {
        public Token Open { get; } = open;

        public IReadOnlyList<Expr> Values { get; } = values;

        public Token Close { get; } = close;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprRange(
        Expr start,
        Token colon1,
        Expr end,
        Token? colon2,
        Expr? step) : Expr
    {
        public Expr Start { get; } = start;

        public Token Colon1 { get; } = colon1;

        public Expr End { get; } = end;

        public Token? Colon2 { get; } = colon2;

        public Expr? Step { get; } = step;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprIndex(
        Expr target,
        Token open,
        Expr index,
        Token close) : Expr
    {
        public Expr Target { get; } = target;

        public Token Open { get; } = open;

        public Expr Index { get; } = index;

        public Token Close { get; } = close;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprMember(
        Expr target,
        Token dot,
        Token member) : Expr
    {
        public Expr Target { get; } = target;

        public Token Dot { get; } = dot;

        public Token Member { get; } = member;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }

    public sealed class ExprError(
        Token token,
        string message) : Expr
    {
        public Token Token { get; } = token;

        public string Message { get; } = message;

        public override T Accept<T>(INodeVisitor<T> visitor)
            => visitor.Visit(this);
    }
    #endregion expressions
}
