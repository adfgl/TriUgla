using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjMeshCommand(
    string name,
    IReadOnlyList<Value> args) : Obj
    {
        public string Name { get; } = name;

        public IReadOnlyList<Value> Args { get; } = args;

        public override DataKind Kind => DataKind.MeshCommand;

        public override string ToString()
        {
            return Args.Count == 0
                ? Name
                : $"{Name} {string.Join(" ", Args)}";
        }
    }
}
