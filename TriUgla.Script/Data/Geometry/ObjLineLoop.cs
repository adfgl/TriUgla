using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjLineLoop(
    int id,
    List<int> lineIds) : Obj
    {
        public int Id { get; } = id;

        public List<int> LineIds { get; } = lineIds ?? [];

        public override DataKind Kind => DataKind.LineLoop;

        public override string ToString()
            => $"Line Loop({Id}) = {{{string.Join(", ", LineIds)}}}";
    }
}
