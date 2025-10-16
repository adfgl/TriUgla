using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    public static class MathHelper
    {
        public const double DEG2RAD = Math.PI / 180.0;
        public const double RAD2DEG = 180.0 / Math.PI;
        public const double INFINITY = 999999.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreParallel(Vec3 a, Vec3 b, double tolerance = 0.001)
        {
            return Vec3.SquareLength(Vec3.Cross(a, b)) <= tolerance * tolerance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ArePerpendicular(Vec3 a, Vec3 b, double tolerance = 0.001)
        {
            return Math.Abs(Vec3.Dot(a, b)) <= tolerance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(double number, double tolerance)
        {
            if (number >= 0.0 - tolerance)
            {
                return number <= tolerance;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOne(double number, double tolerance)
        {
            return IsZero(number - 1.0, tolerance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqual(double a, double b, double tolerance)
        {
            return IsZero(a - b, tolerance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Resultant(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        public static double ResultantAngle(double x, double y)
        {
            if (y == 0.0)
            {
                if (x > 0.0)
                {
                    return 0.0;
                }

                return Math.PI;
            }

            if (x == 0.0)
            {
                if (y > 0.0)
                {
                    return Math.PI / 2.0;
                }

                return 4.71238898038469;
            }

            double num = Math.Atan(Math.Abs(y / x));
            if (x > 0.0)
            {
                if (y > 0.0)
                {
                    return num;
                }

                if (y < 0.0)
                {
                    return Math.PI * 2.0 - num;
                }
            }
            else
            {
                if (y > 0.0)
                {
                    return Math.PI - num;
                }

                if (y < 0.0)
                {
                    return Math.PI + num;
                }
            }

            if (y > 0.0)
            {
                if (x > 0.0)
                {
                    return num;
                }

                if (x < 0.0)
                {
                    return Math.PI - num;
                }
            }
            else
            {
                if (x > 0.0)
                {
                    return Math.PI * 2.0 - num;
                }

                if (x < 0.0)
                {
                    return Math.PI + num;
                }
            }

            return 0.0;
        }
    }
}
