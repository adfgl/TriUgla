using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Data.Geometry;

namespace TriUgla.Script.Data
{
    public sealed class MeshContext
    {
        public Dictionary<string, Value> Options { get; } = [];
        public List<ObjMeshCommand> Commands { get; } = [];

        public void SetOption(string path, Value value)
        {
            Options[path] = value;
        }

        public void AddCommand(ObjMeshCommand command)
        {
            Commands.Add(command);
        }
    }
}
