using TriUgla.Script;

namespace TriUgla.Tests;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_GmshPointDeclaration_ReturnsExpectedKinds()
    {
        IReadOnlyList<Token> tokens = Tokenize("Point(1) = {0, 0, 0, 1};");

        Assert.Equal(
            [
                TokenKind.Keyword,
                TokenKind.LeftParenthesis,
                TokenKind.Number,
                TokenKind.RightParenthesis,
                TokenKind.Equals,
                TokenKind.LeftBrace,
                TokenKind.Number,
                TokenKind.Comma,
                TokenKind.Number,
                TokenKind.Comma,
                TokenKind.Number,
                TokenKind.Comma,
                TokenKind.Number,
                TokenKind.RightBrace,
                TokenKind.Semicolon,
                TokenKind.EndOfFile
            ],
            tokens.Select(token => token.Kind));
    }

    [Fact]
    public void Tokenize_Numbers_SupportsDecimalsAndExponents()
    {
        IReadOnlyList<Token> tokens = Tokenize("0 1.5 .25 2. 1e3 2.5E-2");

        Assert.Equal(
            ["0", "1.5", ".25", "2.", "1e3", "2.5E-2"],
            tokens.Where(token => token.Kind == TokenKind.Number).Select(token => token.Text));
    }

    [Fact]
    public void Tokenize_IncompleteExponent_LeavesIdentifierForNextToken()
    {
        IReadOnlyList<Token> tokens = Tokenize("1e value");

        Assert.Equal(TokenKind.Number, tokens[0].Kind);
        Assert.Equal("1", tokens[0].Text);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("e", tokens[1].Text);
    }

    [Fact]
    public void Tokenize_Keywords_ReturnsKeywordTokenAndParticularKind()
    {
        IReadOnlyList<Token> tokens = Tokenize("If ElseIf Else EndIf For In EndFor While EndWhile Break Continue Return");

        Assert.All(tokens.Take(tokens.Count - 1), token => Assert.Equal(TokenKind.Keyword, token.Kind));
        Assert.Equal(
            [
                KeywordKind.If,
                KeywordKind.ElseIf,
                KeywordKind.Else,
                KeywordKind.EndIf,
                KeywordKind.For,
                KeywordKind.In,
                KeywordKind.EndFor,
                KeywordKind.While,
                KeywordKind.EndWhile,
                KeywordKind.Break,
                KeywordKind.Continue,
                KeywordKind.Return
            ],
            tokens.Take(tokens.Count - 1).Select(token => token.Keyword));
    }

    [Fact]
    public void Tokenize_GmshStatementVocabulary_RecognizesEveryKeyword()
    {
        const string source =
            "Transfinite Curve Line Loop Plane Surface Using Progression Bump " +
            "Mesh Coherence RenumberMeshNodes RenumberMeshElements All " +
            "Point Spline BSpline Bezier Circle";

        IReadOnlyList<Token> tokens = Tokenize(source);

        Assert.All(tokens.Take(tokens.Count - 1), token => Assert.Equal(TokenKind.Keyword, token.Kind));
        Assert.Equal(
            [
                KeywordKind.Transfinite,
                KeywordKind.Curve,
                KeywordKind.Line,
                KeywordKind.Loop,
                KeywordKind.Plane,
                KeywordKind.Surface,
                KeywordKind.Using,
                KeywordKind.Progression,
                KeywordKind.Bump,
                KeywordKind.Mesh,
                KeywordKind.Coherence,
                KeywordKind.RenumberMeshNodes,
                KeywordKind.RenumberMeshElements,
                KeywordKind.All,
                KeywordKind.Point,
                KeywordKind.Spline,
                KeywordKind.BSpline,
                KeywordKind.Bezier,
                KeywordKind.Circle
            ],
            tokens.Take(tokens.Count - 1).Select(token => token.Keyword));
    }

    [Fact]
    public void Tokenize_KeywordMatching_IsCaseSensitiveAndDoesNotMatchPrefixes()
    {
        IReadOnlyList<Token> tokens = Tokenize("if IfValue If");

        Assert.Equal(
            [TokenKind.Identifier, TokenKind.Identifier, TokenKind.Keyword, TokenKind.EndOfFile],
            tokens.Select(token => token.Kind));
        Assert.Equal(KeywordKind.None, tokens[0].Keyword);
        Assert.Equal(KeywordKind.If, tokens[2].Keyword);
    }

    [Fact]
    public void Keywords_AllContainsEveryParticularKeyword()
    {
        Assert.Equal(Enum.GetValues<KeywordKind>().Length - 1, Keywords.All.Count);
        Assert.DoesNotContain(KeywordKind.None, Keywords.All.Values);
    }

    [Fact]
    public void Tokenize_String_KeepsQuotedSourceText()
    {
        Token token = Tokenize("\"surface \\\"name\\\"\"")[0];

        Assert.Equal(TokenKind.String, token.Kind);
        Assert.Equal("\"surface \\\"name\\\"\"", token.Text);
    }

    [Fact]
    public void Tokenize_UnterminatedString_ReturnsBadToken()
    {
        Token token = Tokenize("\"surface")[0];

        Assert.Equal(TokenKind.BadToken, token.Kind);
    }

    [Fact]
    public void Tokenize_Comments_AreSkipped()
    {
        IReadOnlyList<Token> tokens = Tokenize("a // line\n /* block */ b");

        Assert.Equal(
            [TokenKind.Identifier, TokenKind.Identifier, TokenKind.EndOfFile],
            tokens.Select(token => token.Kind));
        Assert.Equal("b", tokens[1].Text);
        Assert.Equal(2, tokens[1].Span.Line);
    }

    [Fact]
    public void Tokenize_ComparisonOperators_ConsumesBothCharacters()
    {
        IReadOnlyList<Token> tokens = Tokenize("= == ! != < <= > >=");

        Assert.Equal(
            [
                TokenKind.Equals,
                TokenKind.EqualsEquals,
                TokenKind.Bang,
                TokenKind.BangEquals,
                TokenKind.Less,
                TokenKind.LessOrEquals,
                TokenKind.Greater,
                TokenKind.GreaterOrEquals,
                TokenKind.EndOfFile
            ],
            tokens.Select(token => token.Kind));
    }

    [Fact]
    public void Tokenize_LoopRange_ReturnsColonTokens()
    {
        IReadOnlyList<Token> tokens = Tokenize("For i In {1:5:2}");

        Assert.Equal(2, tokens.Count(token => token.Kind == TokenKind.Colon));
        Assert.Equal(KeywordKind.For, tokens[0].Keyword);
        Assert.Equal(KeywordKind.In, tokens[2].Keyword);
    }

    [Fact]
    public void Tokenize_UnknownCharacter_ReturnsBadTokenWithSourceSpan()
    {
        Token token = Tokenize("  @")[0];

        Assert.Equal(TokenKind.BadToken, token.Kind);
        Assert.Equal("@", token.Text);
        Assert.Equal(new TextSpan(2, 1, 1, 3), token.Span);
    }

    static IReadOnlyList<Token> Tokenize(string source) => new Tokenizer(source).Tokenize();
}
