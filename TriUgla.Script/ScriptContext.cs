using TriUgla.Script.Data;

namespace TriUgla.Script
{
    public sealed class ScriptContext
    {
        public GeometryContext Geometry { get; } = new();
        public MeshContext Mesh { get; } = new();
        public List<string> Log { get; } = [];
    }
}
