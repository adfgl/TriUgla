using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjEmbedConstraint(
     EntityKind2D entityKind,
     IReadOnlyList<int> entityIds,
     EntityKind2D containerKind,
     IReadOnlyList<int> containerIds) : Obj
    {
        public EntityKind2D EntityKind { get; } = entityKind;

        public IReadOnlyList<int> EntityIds { get; } = entityIds;

        public EntityKind2D ContainerKind { get; } = containerKind;

        public IReadOnlyList<int> ContainerIds { get; } = containerIds;

        public override DataKind Kind => DataKind.EmbedConstraint;

        public override string ToString()
            => $"{EntityKind} {{{string.Join(", ", EntityIds)}}} In {ContainerKind} {{{string.Join(", ", ContainerIds)}}}";
    }
}
