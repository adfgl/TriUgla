using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data.Geometry
{
    public sealed class ObjPhysicalGroup(
    PhysicalGroupId id,
    PhysicalKind physicalKind,
    string? name,
    IReadOnlyList<int> entityIds)
    : ObjEntity<PhysicalGroupId>(id)
    {
        public PhysicalKind PhysicalKind { get; } = physicalKind;

        public string? Name { get; } = name;

        public IReadOnlyList<int> EntityIds { get; } = entityIds;

        public override DataKind Kind => physicalKind switch
        {
            PhysicalKind.Point => DataKind.PhysicalPoint,
            PhysicalKind.Curve => DataKind.PhysicalCurve,
            PhysicalKind.Surface => DataKind.PhysicalSurface,
            _ => DataKind.Undefined
        };

        public override string ToString()
            => $"Physical {PhysicalKind}({Id}) = {{{string.Join(", ", EntityIds)}}}";
    }
}
