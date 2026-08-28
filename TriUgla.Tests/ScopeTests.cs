using TriUgla.Script;

namespace TriUgla.Tests;

public class ScopeTests
{
    [Fact]
    public void Open_Dispose_RemovesVariablesDeclaredInNestedScope()
    {
        var scope = new Scope();
        scope.Declare("outer");

        using (scope.Open())
        {
            scope.Declare("inner");

            Assert.True(scope.IsDeclared("outer"));
            Assert.True(scope.IsDeclared("inner"));
            Assert.Equal(2, scope.Depth);
        }

        Assert.True(scope.IsDeclared("outer"));
        Assert.False(scope.IsDeclared("inner"));
        Assert.Equal(1, scope.Depth);
    }

    [Fact]
    public void Declare_ReturnsFalseForDuplicateInCurrentScope()
    {
        var scope = new Scope();

        Assert.True(scope.Declare("value"));
        Assert.False(scope.Declare("value"));
    }

    [Fact]
    public void Declare_AllowsShadowingVariableFromOuterScope()
    {
        var scope = new Scope();
        scope.Declare("value");

        using (scope.Open())
        {
            Assert.False(scope.IsDeclaredInCurrentScope("value"));
            Assert.True(scope.Declare("value"));
            Assert.True(scope.IsDeclaredInCurrentScope("value"));
        }
    }

    [Fact]
    public void Open_SupportsNestedUsingScopes()
    {
        var scope = new Scope();

        using (scope.Open())
        {
            scope.Declare("first");

            using (scope.Open())
            {
                scope.Declare("second");
                Assert.True(scope.IsDeclared("first"));
                Assert.True(scope.IsDeclared("second"));
            }

            Assert.True(scope.IsDeclared("first"));
            Assert.False(scope.IsDeclared("second"));
        }
    }

    [Fact]
    public void ScopeLease_Dispose_IsIdempotent()
    {
        var scope = new Scope();
        IDisposable lease = scope.Open();

        lease.Dispose();
        lease.Dispose();

        Assert.Equal(1, scope.Depth);
    }

    [Fact]
    public void Values_CanBeReadAndAssignedAcrossNestedScopes()
    {
        var scope = new Scope();
        scope.Declare("value", 1d);

        using (scope.Open())
        {
            Assert.True(scope.TryGetValue("value", out Value initial));
            Assert.Equal(1, initial.Number);
            Assert.True(scope.TryAssign("value", 2d));
        }

        Assert.True(scope.TryGetValue("value", out Value assigned));
        Assert.Equal(2, assigned.Number);
    }
}
