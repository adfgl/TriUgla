using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Geometry
{
    public readonly struct Mat4
    {
        public readonly double m11, m12, m13, m14;
        public readonly double m21, m22, m23, m24;
        public readonly double m31, m32, m33, m34;
        public readonly double m41, m42, m43, m44;

        public Mat4(
             double m11, double m12, double m13, double m14,
             double m21, double m22, double m23, double m24,
             double m31, double m32, double m33, double m34,
             double m41, double m42, double m43, double m44)
        {
            this.m11 = m11; this.m12 = m12; this.m13 = m13; this.m14 = m14;
            this.m21 = m21; this.m22 = m22; this.m23 = m23; this.m24 = m24;
            this.m31 = m31; this.m32 = m32; this.m33 = m33; this.m34 = m34;
            this.m41 = m41; this.m42 = m42; this.m43 = m43; this.m44 = m44;
        }

        public static Mat4 Identity => new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);

        public bool IsIdentity()
        {
            return m11 == 1 && m12 == 0 && m13 == 0 && m14 == 0 &&
                   m21 == 0 && m22 == 1 && m23 == 0 && m24 == 0 &&
                   m31 == 0 && m32 == 0 && m33 == 1 && m34 == 0 &&
                   m41 == 0 && m42 == 0 && m43 == 0 && m44 == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 Translation(double tx, double ty, double tz) => new(
            1, 0, 0, tx,
            0, 1, 0, ty,
            0, 0, 1, tz,
            0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 Scale(double sx, double sy, double sz) => new(
            sx, 0, 0, 0,
            0, sy, 0, 0,
            0, 0, sz, 0,
            0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 RotationX(double rad)
        {
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new Mat4(
                1, 0, 0, 0,
                0, cos, sin, 0,
                0, -sin, cos, 0,
                0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 RotationY(double rad)
        {
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new Mat4(
                cos, 0, -sin, 0,
                0, 1, 0, 0,
                sin, 0, cos, 0,
                0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 RotationZ(double rad)
        {
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new Mat4(
                cos, -sin, 0, 0,
                sin, cos, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

        public double Get(int row, int col)
        {
            return this[row, col];
        }

        public double this[int row, int col]
        {
            get
            {
                return row switch
                {
                    0 => col switch
                    {
                        0 => m11,
                        1 => m12,
                        2 => m13,
                        3 => m14,
                        _ => throw new IndexOutOfRangeException($"Invalid column index {col}. Valid column indices are 0, 1, 2, and 3."),
                    },
                    1 => col switch
                    {
                        0 => m21,
                        1 => m22,
                        2 => m23,
                        3 => m24,
                        _ => throw new IndexOutOfRangeException($"Invalid column index {col}. Valid column indices are 0, 1, 2, and 3."),
                    },
                    2 => col switch
                    {
                        0 => m31,
                        1 => m32,
                        2 => m33,
                        3 => m34,
                        _ => throw new IndexOutOfRangeException($"Invalid column index {col}. Valid column indices are 0, 1, 2, and 3."),
                    },
                    3 => col switch
                    {
                        0 => m41,
                        1 => m42,
                        2 => m43,
                        3 => m44,
                        _ => throw new IndexOutOfRangeException($"Invalid column index {col}. Valid column indices are 0, 1, 2, and 3."),
                    },
                    _ => throw new IndexOutOfRangeException($"Invalid row index {row}. Valid row indices are 0, 1, 2, and 3."),
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Determinant()
        {
            return
                m11 * (m22 * (m33 * m44 - m34 * m43) - m23 * (m32 * m44 - m34 * m42) + m24 * (m32 * m43 - m33 * m42)) -
                m12 * (m21 * (m33 * m44 - m34 * m43) - m23 * (m31 * m44 - m34 * m41) + m24 * (m31 * m43 - m33 * m41)) +
                m13 * (m21 * (m32 * m44 - m34 * m42) - m22 * (m31 * m44 - m34 * m41) + m24 * (m31 * m42 - m32 * m41)) -
                m14 * (m21 * (m32 * m43 - m33 * m42) - m22 * (m31 * m43 - m33 * m41) + m23 * (m31 * m42 - m32 * m41));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Inverse(out Mat4 inverse)
        {
            double det = Determinant();
            if (det == 0)
            {
                inverse = new Mat4();
                return false;
            }

            det = 1 / det;

            inverse = new Mat4(
                det * +(m22 * (m33 * m44 - m34 * m43) - m23 * (m32 * m44 - m34 * m42) + m24 * (m32 * m43 - m33 * m42)),
                det * -(m12 * (m33 * m44 - m34 * m43) - m13 * (m32 * m44 - m34 * m42) + m14 * (m32 * m43 - m33 * m42)),
                det * +(m12 * (m23 * m44 - m24 * m43) - m13 * (m22 * m44 - m24 * m42) + m14 * (m22 * m43 - m23 * m42)),
                det * -(m12 * (m23 * m34 - m24 * m33) - m13 * (m22 * m34 - m24 * m32) + m14 * (m22 * m33 - m23 * m32)),

                det * -(m21 * (m33 * m44 - m34 * m43) - m23 * (m31 * m44 - m34 * m41) + m24 * (m31 * m43 - m33 * m41)),
                det * +(m11 * (m33 * m44 - m34 * m43) - m13 * (m31 * m44 - m34 * m41) + m14 * (m31 * m43 - m33 * m41)),
                det * -(m11 * (m23 * m44 - m24 * m43) - m13 * (m21 * m44 - m24 * m41) + m14 * (m21 * m43 - m23 * m41)),
                det * +(m11 * (m23 * m34 - m24 * m33) - m13 * (m21 * m34 - m24 * m31) + m14 * (m21 * m33 - m23 * m31)),

                det * +(m21 * (m32 * m44 - m34 * m42) - m22 * (m31 * m44 - m34 * m41) + m24 * (m31 * m42 - m32 * m41)),
                det * -(m11 * (m32 * m44 - m34 * m42) - m12 * (m31 * m44 - m34 * m41) + m14 * (m31 * m42 - m32 * m41)),
                det * +(m11 * (m22 * m44 - m24 * m42) - m12 * (m21 * m44 - m24 * m41) + m14 * (m21 * m42 - m22 * m41)),
                det * -(m11 * (m22 * m34 - m24 * m32) - m12 * (m21 * m34 - m24 * m31) + m14 * (m21 * m32 - m22 * m31)),

                det * -(m21 * (m32 * m43 - m33 * m42) - m22 * (m31 * m43 - m33 * m41) + m23 * (m31 * m42 - m32 * m41)),
                det * +(m11 * (m32 * m43 - m33 * m42) - m12 * (m31 * m43 - m33 * m41) + m13 * (m31 * m42 - m32 * m41)),
                det * -(m11 * (m22 * m43 - m23 * m42) - m12 * (m21 * m43 - m23 * m41) + m13 * (m21 * m42 - m22 * m41)),
                det * +(m11 * (m22 * m33 - m23 * m32) - m12 * (m21 * m33 - m23 * m31) + m13 * (m21 * m32 - m22 * m31)));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 Transpose(Mat4 m) => new(
            m.m11, m.m21, m.m31, m.m41,
            m.m12, m.m22, m.m32, m.m42,
            m.m13, m.m23, m.m33, m.m43,
            m.m14, m.m24, m.m34, m.m44);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 Multiply(Mat4 a, Mat4 b) => new(
            (a.m11 * b.m11) + (a.m12 * b.m21) + (a.m13 * b.m31) + (a.m14 * b.m41),
            (a.m11 * b.m12) + (a.m12 * b.m22) + (a.m13 * b.m32) + (a.m14 * b.m42),
            (a.m11 * b.m13) + (a.m12 * b.m23) + (a.m13 * b.m33) + (a.m14 * b.m43),
            (a.m11 * b.m14) + (a.m12 * b.m24) + (a.m13 * b.m34) + (a.m14 * b.m44),

            (a.m21 * b.m11) + (a.m22 * b.m21) + (a.m23 * b.m31) + (a.m24 * b.m41),
            (a.m21 * b.m12) + (a.m22 * b.m22) + (a.m23 * b.m32) + (a.m24 * b.m42),
            (a.m21 * b.m13) + (a.m22 * b.m23) + (a.m23 * b.m33) + (a.m24 * b.m43),
            (a.m21 * b.m14) + (a.m22 * b.m24) + (a.m23 * b.m34) + (a.m24 * b.m44),

            (a.m31 * b.m11) + (a.m32 * b.m21) + (a.m33 * b.m31) + (a.m34 * b.m41),
            (a.m31 * b.m12) + (a.m32 * b.m22) + (a.m33 * b.m32) + (a.m34 * b.m42),
            (a.m31 * b.m13) + (a.m32 * b.m23) + (a.m33 * b.m33) + (a.m34 * b.m43),
            (a.m31 * b.m14) + (a.m32 * b.m24) + (a.m33 * b.m34) + (a.m34 * b.m44),

            (a.m41 * b.m11) + (a.m42 * b.m21) + (a.m43 * b.m31) + (a.m44 * b.m41),
            (a.m41 * b.m12) + (a.m42 * b.m22) + (a.m43 * b.m32) + (a.m44 * b.m42),
            (a.m41 * b.m13) + (a.m42 * b.m23) + (a.m43 * b.m33) + (a.m44 * b.m43),
            (a.m41 * b.m14) + (a.m42 * b.m24) + (a.m43 * b.m34) + (a.m44 * b.m44));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 Multiply(Mat4 m, Vec3 v) => new(
            m.m11 * v.x + m.m12 * v.y + m.m13 * v.z + m.m14 * v.w,
            m.m21 * v.x + m.m22 * v.y + m.m23 * v.z + m.m24 * v.w,
            m.m31 * v.x + m.m32 * v.y + m.m33 * v.z + m.m34 * v.w,
            m.m41 * v.x + m.m42 * v.y + m.m43 * v.z + m.m44 * v.w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 Multiply(Mat4 m, double scalar) => new(
            m.m11 * scalar, m.m12 * scalar, m.m13 * scalar, m.m14 * scalar,
            m.m21 * scalar, m.m22 * scalar, m.m23 * scalar, m.m24 * scalar,
            m.m31 * scalar, m.m32 * scalar, m.m33 * scalar, m.m34 * scalar,
            m.m41 * scalar, m.m42 * scalar, m.m43 * scalar, m.m44 * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 Divide(Mat4 m, double scalar) => new(
            m.m11 / scalar, m.m12 / scalar, m.m13 / scalar, m.m14 / scalar,
            m.m21 / scalar, m.m22 / scalar, m.m23 / scalar, m.m24 / scalar,
            m.m31 / scalar, m.m32 / scalar, m.m33 / scalar, m.m34 / scalar,
            m.m41 / scalar, m.m42 / scalar, m.m43 / scalar, m.m44 / scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 operator *(Mat4 a, Mat4 b) => Multiply(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator *(Mat4 m, Vec3 v) => Multiply(m, v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator *(Vec3 v, Mat4 m) => Multiply(m, v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 operator *(Mat4 m, double scalar) => Multiply(m, scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 operator *(double scalar, Mat4 m) => Multiply(m, scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat4 operator /(Mat4 m, double scalar) => Divide(m, scalar);

        public string ToString(int precision)
        {
            string format = $"F{precision}";

            int colWidth = Math.Max(
                Math.Max(m11.ToString(format).Length, m12.ToString(format).Length),
                Math.Max(m13.ToString(format).Length, m14.ToString(format).Length)
            );

            string paddedFormat = $"{{0,{colWidth}:{format}}}";

            return string.Format(
                "{0} {1} {2} {3}\n{4} {5} {6} {7}\n{8} {9} {10} {11}\n{12} {13} {14} {15}",
                string.Format(paddedFormat, m11), string.Format(paddedFormat, m12), string.Format(paddedFormat, m13), string.Format(paddedFormat, m14),
                string.Format(paddedFormat, m21), string.Format(paddedFormat, m22), string.Format(paddedFormat, m23), string.Format(paddedFormat, m24),
                string.Format(paddedFormat, m31), string.Format(paddedFormat, m32), string.Format(paddedFormat, m33), string.Format(paddedFormat, m34),
                string.Format(paddedFormat, m41), string.Format(paddedFormat, m42), string.Format(paddedFormat, m43), string.Format(paddedFormat, m44)
            );
        }

        public override string ToString()
        {
            return ToString(2);
        }

        public static Mat4 Alignment(Vec3 actualNormal, Vec3 targetNormal)
        {
            actualNormal = actualNormal.Normalize();
            targetNormal = targetNormal.Normalize();

            if (actualNormal == targetNormal)
            {
                return Mat4.Identity;
            }

            Vec3 axis = Vec3.Cross(actualNormal, targetNormal);

            Mat4 mat;
            if (axis.IsZero()) // Happens when actualNormal == -targetNormal
            {
                axis = Math.Abs(actualNormal.x) < 0.9 ? new Vec3(1, 0, 0) : new Vec3(0, 1, 0);
                axis = Vec3.Cross(axis, actualNormal).Normalize(); // Ensure it's truly perpendicular
                mat = FromRodrigues(axis, Math.PI); // 180-degree rotation
            }
            else
            {
                double angle = Math.Acos(Vec3.Dot(actualNormal, targetNormal));
                mat = FromRodrigues(axis, angle);
            }

            Vec3 trans = actualNormal * mat;
            if (!MathHelper.AreEqual(trans.x, targetNormal.x, 0.00001) ||
                !MathHelper.AreEqual(trans.x, targetNormal.x, 0.00001) ||
                !MathHelper.AreEqual(trans.x, targetNormal.x, 0.00001))
            {
                throw new InvalidOperationException("Transformation matrix is incorrect.");
            }
            return mat;
        }

        /// <summary>
        /// Converts a rotation specified by an axis and an angle into a 4x4 rotation matrix.
        /// </summary>
        /// <param name="axis">The axis of rotation as a normalized vector (Vec3).</param>
        /// <param name="angle">The angle of rotation in radians.</param>
        /// <returns>A Mat4 rotation matrix representing the rotation described by the axis and angle.</returns>
        /// <remarks>
        /// The Rodrigues' rotation formula is used to calculate the rotation matrix.
        /// This method assumes the axis is normalized, and the angle is in radians.
        /// </remarks>
        public static Mat4 FromRodrigues(Vec3 axis, double angle)
        {
            axis = axis.Normalize();
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);
            double oneMinusC = 1 - c;

            double ux = axis.x, uy = axis.y, uz = axis.z;
            return new Mat4(
                c + ux * ux * oneMinusC, ux * uy * oneMinusC - uz * s, ux * uz * oneMinusC + uy * s, 0,
                uy * ux * oneMinusC + uz * s, c + uy * uy * oneMinusC, uy * uz * oneMinusC - ux * s, 0,
                uz * ux * oneMinusC - uy * s, uz * uy * oneMinusC + ux * s, c + uz * uz * oneMinusC, 0,
              0, 0, 0, 1);
        }
    }
}
