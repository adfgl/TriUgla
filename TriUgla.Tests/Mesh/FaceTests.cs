namespace TriUgla.Tests;

public class FaceTests
{
    [Fact]
    public void AreaComputesTriangleArea()
    {
        Face face = Triangle(new Vec2(0, 0), new Vec2(4, 0), new Vec2(0, 3));

        Assert.Equal(6, face.SignedArea);
        Assert.Equal(6, face.Area);
    }

    [Fact]
    public void SignedAreaPreservesWindingWhileAreaStaysPositive()
    {
        Face face = Triangle(new Vec2(0, 0), new Vec2(0, 3), new Vec2(4, 0));

        Assert.Equal(-6, face.SignedArea);
        Assert.Equal(6, face.Area);
    }

    [Fact]
    public void UnlinkedFaceHasZeroArea()
    {
        Face face = new();

        Assert.Equal(0, face.SignedArea);
        Assert.Equal(0, face.Area);
    }

    static Face Triangle(Vec2 a, Vec2 b, Vec2 c)
    {
        var face = new Face();
        Linker.LinkTriangle(
            face,
            new Edge(), new Edge(), new Edge(),
            new Node { Position = a },
            new Node { Position = b },
            new Node { Position = c });
        return face;
    }
}
