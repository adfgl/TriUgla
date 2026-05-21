namespace TriUgla.Script
{
    public interface INodeVisitor<T>
    {
        T Visit(StmtProg node);
        T Visit(ExprBinary node);
        T Visit(ExprGroup node);
        T Visit(ExprNumber node);
        T Visit(ExprString node);
        T Visit(ExprBoolean node);
    }

    public abstract class Node
    {
        public abstract T Accept<T>(INodeVisitor<T> visitor);
    }

    public abstract class Expr : Node { }
    public abstract class Stmt : Node { }

    public sealed class StmtProg(List<Stmt> statements) : Stmt
    {
        public IReadOnlyList<Stmt> Statements { get; } = statements;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class ExprGroup(Token open, Expr inner, Token close) : Expr
    {
        public Token Open { get; } = open;
        public Expr Inner { get; } = inner;
        public Token Close { get; } = close;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class ExprNumber(Token token, double value) : Expr
    {
        public Token Token { get; } = token;
        public double Value { get; } = value;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class ExprString(Token token, string value) : Expr
    {
        public Token Token { get; } = token;
        public string Value { get; } = value;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class ExprBoolean(Token token, bool value) : Expr
    {
        public Token Token { get; } = token;
        public bool Value { get; } = value;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }

    public sealed class ExprBinary(Expr left, Token op, Expr right) : Expr
    {
        public Expr Left { get; } = left;
        public Token Op { get; } = op;
        public Expr Right { get; } = right;
        public override T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    }
}
