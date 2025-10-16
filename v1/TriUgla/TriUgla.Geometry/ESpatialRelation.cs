using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    [Flags]
    public enum ESpatialRelation
    {
        None = 0,
        All = Parallel | Perpendicular | Skew | Opposite | Identical,

        /// <summary>
        /// Vectors are collinear (same or opposite direction)
        /// </summary>
        Parallel = 1 << 0,

        /// <summary>
        /// Vectors are orthogonal
        /// </summary>
        Perpendicular = 1 << 1,

        /// <summary>
        /// Neither parallel nor intersecting
        /// </summary>
        Skew = 1 << 2,

        /// <summary>
        /// Collinear but in opposite directions
        /// </summary>
        Opposite = 1 << 3,

        /// <summary>
        /// Exactly the same vector
        /// </summary>
        Identical = 1 << 4,
    }
}
