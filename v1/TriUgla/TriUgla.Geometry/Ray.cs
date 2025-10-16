using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    public readonly struct Ray
    {
        public readonly Vec3 origin, direction;

        public Ray(Vec3 origin, Vec3 direction)
        {
            this.origin = origin;
            this.direction = direction.Normalize();
        }

        public static Ray FromLine(Vec3 start, Vec3 end)
        {
            return new Ray(start, end - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3 PointAlong(double distance) => origin + direction * distance;

        public static Ray PerturbDirection(Ray ray, double pertrubation = 1e-10)
        {
            double x = ray.direction.x + pertrubation;
            double y = ray.direction.y + pertrubation;
            double z = ray.direction.z + pertrubation;
            return new Ray(ray.origin, new Vec3(x, y, z).Normalize());
        }

        public static Ray PerturbDirection(Ray ray, Random random, double pertrubation = 1e-10)
        {
            double x = ray.direction.x + pertrubation * random.NextDouble();
            double y = ray.direction.y + pertrubation * random.NextDouble();
            double z = ray.direction.z + pertrubation * random.NextDouble();
            return new Ray(ray.origin, new Vec3(x, y, z).Normalize());
        }

        public static bool Intersect(Ray ray, Vec3 a, Vec3 b, out Vec3 intersection, double tolerance = 0)
        {
            /*
                Solve for t and s where:
                O + tD = A + s(B - A)

                Rearranging:
                tD - s(B - A) = A - O

                This is a system of equations with two unknowns (t and s).
                We solve it using the determinant method.
            */

            intersection = Vec3.NaN;

            Vec3 d = ray.direction;
            Vec3 ab = b - a;
            Vec3 ao = a - ray.origin;

            // Solve using determinant method
            Vec3 crossDirAB = Vec3.Cross(d, ab);
            double denom = Vec3.SquareLength(crossDirAB);

            if (MathHelper.IsZero(denom, tolerance)) // Parallel case
                return false;

            double t = Vec3.Dot(Vec3.Cross(ao, ab), crossDirAB) / denom;
            double s = Vec3.Dot(Vec3.Cross(ao, d), crossDirAB) / denom;

            // Check if intersection is within valid range
            if (t < 0 || s < 0 || s > 1)
                return false;

            intersection = ray.PointAlong(t);
            return true;
        }

        public static bool Intersect(Ray ray, Plane3 plane, out Vec3 intersection, double tolerance = 0)
        {
            /* 
                R(t) = O + t * D
                +-> O is the origin of the ray
                +-> D is the direction of the ray
                +-> t is a scalar value allowing to get point along the ray

                n * p = D0
                +-> n is the normal of the plane
                +-> p is the point on the plane
                +-> D0 distance from the origin to the plane

                The goal is to find such 't' which will satisfy this condition:

                1. x = O + t * D
                2. x = D0 / n
                3. O + t * D = D0 / n
                4. t = (D0 / n - O) / D >> (D0 - O * n) / (D * n)
            */

            intersection = Vec3.NaN;

            double signedDistance = plane.SignedDistanceTo(ray.origin);
            if (MathHelper.IsZero(signedDistance, tolerance))
            {
                intersection = ray.origin;
                return true;
            }

            double dot = Vec3.Dot(plane.normal, ray.direction); // which is 'D * n'
            if (MathHelper.IsZero(dot, tolerance) // parallel to plane
                ||
                (Math.Sign(dot) == Math.Sign(signedDistance))) // pointing away
            {
                return false;
            }

            double t = -signedDistance / dot;
            intersection = ray.PointAlong(t);
            return true;
        }

        public static bool Intersect(Ray ray, Vec3 point, out Vec3 intersection, double tolerance = 1e-6)
        {
            Vec3 op = point - ray.origin;
            double t = Vec3.Dot(op, ray.direction);
            // The point must be in the forward direction of the ray (t >= 0)
            if (t < 0)
            {
                intersection = Vec3.NaN;
                return false;
            }

            // Compute the closest point on the ray
            Vec3 closestPoint = ray.origin + t * ray.direction;
            intersection = closestPoint;
            return
                MathHelper.AreEqual(point.x, closestPoint.x, tolerance) &&
                MathHelper.AreEqual(point.y, closestPoint.y, tolerance) &&
                MathHelper.AreEqual(point.z, closestPoint.z, tolerance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3 Project(Vec3 point)
        {
            Vec3 toPoint = point - origin;
            double t = Vec3.Dot(toPoint, direction); // scalar projection
            return origin + direction * t;           // point along ray
        }
    }
}
