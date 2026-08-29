using TriUgla.Script;

namespace TriUgla.Tests;

public class ScriptDocumentationTests
{
    [Fact]
    public void Catalog_DocumentsEveryKeyword()
    {
        foreach (string keyword in Keywords.All.Keys)
        {
            Assert.True(ScriptDocumentation.TryGet(keyword, out _), $"Missing documentation for keyword '{keyword}'.");
        }
    }

    [Fact]
    public void Catalog_DocumentsEveryStandardFunction()
    {
        foreach (string function in StandardMathFunctions.Names)
        {
            Assert.True(ScriptDocumentation.TryGet(function, out _), $"Missing documentation for function '{function}'.");
        }
    }

    [Fact]
    public void Catalog_DocumentsEveryMeshProperty()
    {
        foreach (string property in new MeshScriptModel().PropertyNames)
        {
            Assert.True(ScriptDocumentation.TryGet($"Mesh.{property}", out ScriptDocumentationEntry? entry));
            Assert.False(string.IsNullOrWhiteSpace(entry.Description));
            Assert.False(string.IsNullOrWhiteSpace(entry.AcceptedValues));
        }
    }
}
