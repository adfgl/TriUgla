using TriUgla.Script;

namespace TriUgla.Tests;

public class AstTests
{
    [Fact]
    public void ExpressionsDeriveFromExpr()
    {
        Expr expression = new NameExpr(Token(TokenKind.Identifier, "value", 0));

        Assert.IsType<NameExpr>(expression);
        Assert.IsAssignableFrom<AstNode>(expression);
    }

    [Fact]
    public void StatementsDeriveFromStmt()
    {
        Expr expression = new NameExpr(Token(TokenKind.Identifier, "value", 0));
        Stmt statement = new ExpressionStmt(
            expression,
            Token(TokenKind.Semicolon, ";", 5));

        Assert.IsType<ExpressionStmt>(statement);
        Assert.IsAssignableFrom<AstNode>(statement);
    }

    [Fact]
    public void AssignmentStatementRepresentsGmshStyleDeclaration()
    {
        var name = new NameExpr(Token(TokenKind.Identifier, "Point", 0));
        var id = new LiteralExpr(Token(TokenKind.Number, "1", 6), 1d);
        var target = new CallExpr(
            name,
            Token(TokenKind.LeftParenthesis, "(", 5),
            [id],
            Token(TokenKind.RightParenthesis, ")", 7));
        var values = new ListExpr(
            Token(TokenKind.LeftBrace, "{", 11),
            [new LiteralExpr(Token(TokenKind.Number, "0", 12), 0d)],
            Token(TokenKind.RightBrace, "}", 13));

        var statement = new AssignmentStmt(
            target,
            Token(TokenKind.Equals, "=", 9),
            values,
            Token(TokenKind.Semicolon, ";", 14));

        Assert.Same(target, statement.Target);
        Assert.Same(values, statement.Value);
        Assert.Equal(new TextSpan(0, 15, 1, 1), statement.Span);
    }

    [Fact]
    public void BinaryExpressionSpanIncludesBothOperands()
    {
        Expr left = new LiteralExpr(Token(TokenKind.Number, "1", 2), 1d);
        Expr right = new LiteralExpr(Token(TokenKind.Number, "2", 6), 2d);

        var expression = new BinaryExpr(
            left,
            Token(TokenKind.Plus, "+", 4),
            right);

        Assert.Equal(new TextSpan(2, 5, 1, 3), expression.Span);
    }

    [Fact]
    public void GroupExpressionSpanIncludesParentheses()
    {
        Expr inner = new NameExpr(Token(TokenKind.Identifier, "value", 1));

        var expression = new GroupExpr(
            Token(TokenKind.LeftParenthesis, "(", 0),
            inner,
            Token(TokenKind.RightParenthesis, ")", 6));

        Assert.Same(inner, expression.Expression);
        Assert.Equal(new TextSpan(0, 7, 1, 1), expression.Span);
    }

    [Fact]
    public void CompilationUnitSpanCoversStatementsThroughEndOfFile()
    {
        var statement = new ExpressionStmt(
            new NameExpr(Token(TokenKind.Identifier, "value", 0)),
            Token(TokenKind.Semicolon, ";", 5));
        Token eof = new(TokenKind.EndOfFile, string.Empty, new TextSpan(6, 0, 1, 7));

        var unit = new CompilationUnit([statement], eof);

        Assert.Equal(new TextSpan(0, 6, 1, 1), unit.Span);
    }

    static Token Token(TokenKind kind, string text, int start)
        => new(kind, text, new TextSpan(start, text.Length, 1, start + 1));
}
