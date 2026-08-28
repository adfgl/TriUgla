using TriUgla.Script;

namespace TriUgla.Tests;

public class ValueTests
{
    [Fact]
    public void Number_StoresDoubleInline()
    {
        Value value = 42.5;

        Assert.True(value.IsNumber);
        Assert.False(value.IsObject);
        Assert.Equal(ValueKind.Number, value.Kind);
        Assert.Equal(42.5, value.Number);
    }

    [Fact]
    public void Object_StoresScriptObjectReference()
    {
        var instance = new CustomObject(42);
        Value value = instance;

        Assert.True(value.IsObject);
        Assert.False(value.IsNumber);
        Assert.Same(instance, value.Object);
        Assert.Same(instance, value.As<CustomObject>());
    }

    [Fact]
    public void String_CreatesScriptString()
    {
        Value value = "surface";

        Assert.Equal("surface", value.As<ScriptString>().Value);
        Assert.Equal("surface", value.ToString());
    }

    [Fact]
    public void ReadingWrongStorageKind_Throws()
    {
        Value number = 1d;
        Value text = "value";

        Assert.Throws<InvalidOperationException>(() => number.Object);
        Assert.Throws<InvalidOperationException>(() => text.Number);
    }

    [Fact]
    public void As_WithWrongObjectType_Throws()
    {
        Value value = "value";

        Assert.Throws<InvalidCastException>(() => value.As<CustomObject>());
    }

    [Fact]
    public void LiteralExpression_UsesValue()
    {
        Token token = new(
            TokenKind.Number,
            "12",
            new TextSpan(0, 2, 1, 1));

        var expression = new LiteralExpr(token, 12d);

        Assert.Equal(12, expression.Value.Number);
    }

    sealed class CustomObject(int id) : ScriptObject
    {
        public int Id { get; } = id;
    }
}
