using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    public static class VecEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ESpatialRelation Relation<T>(T a, T b, Func<T, double> squareLength, Func<T, T, double> dot, double tolerance = 0)
        {
            double magSqA = squareLength(a);
            double magSqB = squareLength(b);
            double normFactor = Math.Sqrt(magSqA * magSqB);
            if (MathHelper.IsZero(normFactor, tolerance))
            {
                return ESpatialRelation.None;
            }

            double dotProduct = dot(a, b) / normFactor;
            if (MathHelper.IsOne(dotProduct, tolerance))
            {
                return ESpatialRelation.Parallel | ESpatialRelation.Identical;
            }

            if (MathHelper.IsOne(-dotProduct, tolerance))
            {
                return ESpatialRelation.Parallel | ESpatialRelation.Opposite;
            }

            if (MathHelper.IsZero(dotProduct, tolerance))
            {
                return ESpatialRelation.Perpendicular;
            }

            return ESpatialRelation.Skew;
        }

        public static ESpatialRelation Relation(this Vec2 a, Vec2 b, double tolerance = 0)
        {
            return Relation(a, b, Vec2.SquareLength, Vec2.Dot, tolerance);
        }

        public static ESpatialRelation Relation(this Vec3 a, Vec3 b, double tolerance = 0)
        {
            return Relation(a, b, Vec3.SquareLength, Vec3.Dot, tolerance);
        }
    }
}
