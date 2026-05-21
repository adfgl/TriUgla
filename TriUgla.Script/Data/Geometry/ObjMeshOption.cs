using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjMeshOption(
    string path,
    Value value) : Obj
    {
        public string Path { get; } = path;

        public Value Value { get; } = value;

        public override DataKind Kind => DataKind.MeshOption;

        public override string ToString()
            => $"{Path} = {Value}";
    }
}
