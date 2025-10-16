using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    public readonly struct Box
    {
        public readonly double dx, dy, dz;
        public readonly double minX, minY, minZ;
        public readonly double maxX, maxY, maxZ;

        public static Box Empty => new Box(double.MaxValue, double.MaxValue, double.MaxValue, double.MinValue, double.MinValue, double.MinValue);

        public Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            this.minX = minX;
            this.minY = minY;
            this.minZ = minZ;
            this.maxX = maxX;
            this.maxY = maxY;
            this.maxZ = maxZ;

            this.dx = maxX - minX;
            this.dy = maxY - minY;
            this.dz = maxZ - minZ;
        }

        public Box Expand(double value)
        {
            value = Math.Abs(value);
            return new Box(minX - value, minY - value, minZ - value, maxX + value, maxY + value, maxZ + value);
        }

        public bool IsEmpty()
        {
            return minX > maxX || minY > maxY || minZ > maxZ;
        }

        public static Box FromPoints(params Vec3[] points)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            foreach (var point in points)
            {
                var (x, y, z) = point;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                minZ = Math.Min(minZ, z);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                maxZ = Math.Max(maxZ, z);
            }

            return new Box(minX, minY, minZ, maxX, maxY, maxZ);
        }

        public static Box FromPoints(IEnumerable<Vec3> points)
        {
            return FromPoints(points.ToArray());
        }

        public static Box Build(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            return new Box(
                Math.Min(minX, maxX), Math.Min(minY, maxY), Math.Min(minZ, maxZ),
                Math.Max(minX, maxX), Math.Max(minY, maxY), Math.Max(minZ, maxZ)
            );
        }

        public void Deconstruct(out Vec3 min, out Vec3 max)
        {
            min = new Vec3(minX, minY, minZ);
            max = new Vec3(maxX, maxY, maxZ);
        }

        public Box Move(double dx, double dy, double dz)
        {
            return new Box(minX + dx, minY + dy, minZ + dz, maxX + dx, maxY + dy, maxZ + dz);
        }

        public Vec3 Center() => new Vec3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);

        public bool Contains(double x, double y, double z) =>
            x >= minX && x <= maxX && y >= minY && y <= maxY && z >= minZ && z <= maxZ;

        public bool ContainsStrict(double x, double y, double z) =>
            x > minX && x < maxX && y > minY && y < maxY && z > minZ && z < maxZ;

        public Box Union(double x, double y, double z)
        {
            return new Box(
                Math.Min(minX, x), Math.Min(minY, y), Math.Min(minZ, z),
                Math.Max(maxX, x), Math.Max(maxY, y), Math.Max(maxZ, z)
            );
        }

        public Box Union(Vec3 v) => Union(v.x, v.y, v.z);

        public bool Contains(Box other) =>
            minX <= other.minX && minY <= other.minY && minZ <= other.minZ &&
            maxX >= other.maxX && maxY >= other.maxY && maxZ >= other.maxZ;

        public bool ContainsStrict(Box other) =>
            minX < other.minX && minY < other.minY && minZ < other.minZ &&
            maxX > other.maxX && maxY > other.maxY && maxZ > other.maxZ;

        public bool Intersects(Box other) =>
            minX <= other.maxX && minY <= other.maxY && minZ <= other.maxZ &&
            maxX >= other.minX && maxY >= other.minY && maxZ >= other.minZ;

        public bool IntersectsStrict(Box other) =>
            minX < other.maxX && minY < other.maxY && minZ < other.maxZ &&
            maxX > other.minX && maxY > other.minY && maxZ > other.minZ;

        public Box Union(Box other)
        {
            return new Box(
                Math.Min(minX, other.minX), Math.Min(minY, other.minY), Math.Min(minZ, other.minZ),
                Math.Max(maxX, other.maxX), Math.Max(maxY, other.maxY), Math.Max(maxZ, other.maxZ)
            );
        }

        public bool Intersection(Box other, out Box intersection)
        {
            double minX = Math.Max(this.minX, other.minX);
            double minY = Math.Max(this.minY, other.minY);
            double minZ = Math.Max(this.minZ, other.minZ);
            double maxX = Math.Min(this.maxX, other.maxX);
            double maxY = Math.Min(this.maxY, other.maxY);
            double maxZ = Math.Min(this.maxZ, other.maxZ);

            if (minX <= maxX && minY <= maxY && minZ <= maxZ)
            {
                intersection = new Box(minX, minY, minZ, maxX, maxY, maxZ);
                return true;
            }

            intersection = Empty;
            return false;
        }

        /// <summary>
        /// 0 - intersects, 1 - in foront, -1 - behind
        /// </summary>
        /// <param name="box"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static int Relation(Box box, Plane3 plane)
        {
            Vec3[] points =
            [
                new Vec3(box.minX, box.minY, box.minZ),
                new Vec3(box.minX, box.minY, box.maxZ),
                new Vec3(box.minX, box.maxY, box.minZ),
                new Vec3(box.minX, box.maxY, box.maxZ),
                new Vec3(box.maxX, box.minY, box.minZ),
                new Vec3(box.maxX, box.minY, box.maxZ),
                new Vec3(box.maxX, box.maxY, box.minZ),
                new Vec3(box.maxX, box.maxY, box.maxZ)
            ];

            bool positiveFound = false, negativeFound = false;
            foreach (Vec3 pt in points)
            {
                double dist = plane.SignedDistanceTo(pt);
                if (!positiveFound && dist > 0) positiveFound = true;
                if (!negativeFound && dist < 0) negativeFound = true;
                if (positiveFound && negativeFound)
                    return 0;
            }
            return positiveFound ? 1 : -1;
        }
    }
}
