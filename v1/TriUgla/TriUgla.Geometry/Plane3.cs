using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    public readonly struct Plane3
    {
        public readonly Vec3 normal;
        public readonly double distanceToOrigin;

        public static readonly Plane3 NaN = new Plane3(Vec3.NaN, double.NaN);

        public Plane3(Vec3 normal, double distanceToOrigin)
        {
            this.normal = normal.Normalize();
            this.distanceToOrigin = distanceToOrigin;
        }

        public Plane3(Vec3 a, Vec3 b, Vec3 c)
        {
            this.normal = Vec3.Cross(b - a, c - a).Normalize();
            this.distanceToOrigin = Vec3.Dot(normal, a);
        }

        public Plane3(Vec3 point, Vec3 normal) : this(normal, Vec3.Dot(normal, point))
        {

        }

        public bool IsValid()
        {
            return Vec3.SquareLength(normal) > 0.0;
        }

        /// <summary>
        /// Calculates the signed distance from a specified point to the plane.
        /// </summary>
        /// <param name="point"></param>
        /// <returns>The distance is <c>positive</c> if the point is in the direction of the plane's normal, <c>negative</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double SignedDistanceTo(Vec3 point)
        {
            return Vec3.Dot(normal, point) - distanceToOrigin;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3 PointOnPlane() => normal * distanceToOrigin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3 Project(Vec3 point) => point - SignedDistanceTo(point) * normal;

        public Plane3 Flip() => new Plane3(-normal, -distanceToOrigin);

        /// <summary>
        /// 0 - on plane, 1 in front, -1 behind
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static int Relation(Plane3 plane, Vec3 point, double tolerance = 0)
        {
            double dist = plane.SignedDistanceTo(point);
            if (MathHelper.IsZero(dist, tolerance))
            {
                return 0;
            }
            return dist > 0 ? 1 : -1;
        }

        public static bool Intersect(Plane3 plane, Vec3 start, Vec3 end, out Vec3 intersection, double tolerance = 0)
        {
            intersection = Vec3.NaN;

            double startDistance = plane.SignedDistanceTo(start);
            if (MathHelper.IsZero(startDistance, tolerance))
            {
                intersection = start;
                return true;
            }

            double endDistance = plane.SignedDistanceTo(end);
            if (MathHelper.IsZero(endDistance, tolerance))
            {
                intersection = end;
                return true;
            }

            if (Math.Sign(startDistance) == Math.Sign(endDistance))
            {
                return false;
            }

            Vec3 lineDir = (end - start).Normalize();
            double dot = Vec3.Dot(plane.normal, lineDir);

            double t = -startDistance / dot;
            intersection = start + lineDir * t;
            return true;
        }

        public static bool Intersect(Plane3 a, Plane3 b, out Ray ray)
        {
            Vec3 direction = Vec3.Cross(a.normal, b.normal);

            double denom = Vec3.SquareLength(direction);
            if (MathHelper.IsZero(denom, 0.001))
            {
                // Normals are parallel (planes are parallel or coincident)
                ray = new Ray();
                return false;
            }

            // Solve system:
            //    Dot(a.normal, P) = a.distanceToOrigin
            //    Dot(b.normal, P) = b.distanceToOrigin
            //
            // Rewrite it into: P = c1 * a.normal + c2 * b.normal + c3 * direction
            // Find point along the intersection line by solving the above system
            Vec3 n1 = a.normal;
            Vec3 n2 = b.normal;
            double d1 = a.distanceToOrigin;
            double d2 = b.distanceToOrigin;

            Vec3 n1xn2 = direction;
            Vec3 n1xn2_cross_n2 = Vec3.Cross(n1xn2, n2);
            Vec3 n1xn2_cross_n1 = Vec3.Cross(n1, n1xn2);

            Vec3 p = ((d1 * n1xn2_cross_n2) + (d2 * n1xn2_cross_n1)) / denom;
            ray = new Ray(direction, p);
            return true;
        }
    }
}
