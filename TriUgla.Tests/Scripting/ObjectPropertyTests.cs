using TriUgla.Script;

namespace TriUgla.Tests;

public class ObjectPropertyTests
{
    [Fact]
    public void Evaluate_ObjectProperty_CanBeReadAndWrittenWithoutMeshSpecificSyntax()
    {
        var settings = new TestSettings();
        var scope = new Scope();
        scope.Declare("Settings", settings);
        var evaluator = new EvaluationVisitor(scope);

        Value result = evaluator.Evaluate(
            SyntaxTree.Parse("Settings.Scale = 3; Settings.Scale + 1;").Root);

        Assert.Equal(4, result.Number);
        Assert.Equal(3, settings.Scale);
    }

    [Fact]
    public void Parse_ChainedObjectProperties_ProducesMemberAccessExpressions()
    {
        SyntaxTree tree = SyntaxTree.Parse("first.second.third;");

        var statement = Assert.IsType<ExpressionStmt>(Assert.Single(tree.Root.Statements));
        var outer = Assert.IsType<MemberAccessExpr>(statement.Expression);
        Assert.Equal("third", outer.Member.Text);
        Assert.IsType<MemberAccessExpr>(outer.Target);
        Assert.Empty(tree.Diagnostics);
    }

    [Fact]
    public void Evaluate_MissingObjectPropertyDefinition_ThrowsFocusedError()
    {
        var scope = new Scope();
        scope.Declare("Settings", new TestSettings());
        var evaluator = new EvaluationVisitor(scope);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            evaluator.Evaluate(SyntaxTree.Parse("Settings.Unknown;").Root));

        Assert.Contains("does not contain a property definition named 'Unknown'", exception.Message);
    }

    [Fact]
    public void ScriptObject_ListsItsPropertyDefinitions()
    {
        Assert.Equal(["Scale"], new TestSettings().PropertyNames);
        Assert.Contains("ElementOrder", new MeshScriptModel().PropertyNames);
    }

    sealed class TestSettings : ScriptObject
    {
        public double Scale { get; private set; }
        public override IReadOnlyList<string> PropertyNames => [nameof(Scale)];

        public override Value GetProperty(string name)
            => name == nameof(Scale)
                ? Scale
                : base.GetProperty(name);

        public override void SetProperty(string name, Value value)
        {
            if (name != nameof(Scale))
            {
                base.SetProperty(name, value);
                return;
            }

            Scale = value.Number;
        }
    }
}
