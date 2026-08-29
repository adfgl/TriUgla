using TriUgla.Script;

namespace TriUgla.Tests;

public class TokenReaderTests
{
    [Fact]
    public void Read_ReturnsOneTokenAtATime()
    {
        var reader = new TokenReader("Point(1)");

        Assert.Equal(TokenKind.Keyword, reader.Read().Kind);
        Assert.Equal(TokenKind.LeftParenthesis, reader.Read().Kind);
        Assert.Equal(TokenKind.Number, reader.Read().Kind);
        Assert.Equal(TokenKind.RightParenthesis, reader.Read().Kind);
        Assert.Equal(TokenKind.EndOfFile, reader.Read().Kind);
    }

    [Fact]
    public void Peek_ReturnsNextTokenWithoutConsumingIt()
    {
        var reader = new TokenReader("Point");

        Token firstPeek = reader.Peek();
        Token secondPeek = reader.Peek();
        Token read = reader.Read();

        Assert.Equal(firstPeek, secondPeek);
        Assert.Equal(firstPeek, read);
        Assert.Equal(TokenKind.EndOfFile, reader.Peek().Kind);
    }

    [Fact]
    public void Read_ConsumesStoredLookAheadBeforeReadingNextToken()
    {
        var reader = new TokenReader("first second");

        Assert.Equal("first", reader.Peek().Text);
        Assert.Equal("first", reader.Read().Text);
        Assert.Equal("second", reader.Read().Text);
    }

    [Fact]
    public void Read_AfterEndOfFile_ContinuesReturningEndOfFile()
    {
        var reader = new TokenReader(string.Empty);

        Token first = reader.Read();
        Token second = reader.Read();

        Assert.Equal(TokenKind.EndOfFile, first.Kind);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Constructor_TokenSequenceWithoutEndOfFile_Throws()
    {
        Token[] tokens =
        [
            new Token(TokenKind.Identifier, "value", new TextSpan(0, 5, 1, 1))
        ];

        Assert.Throws<ArgumentException>(() => new TokenReader(tokens));
    }
}
