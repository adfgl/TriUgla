using TriUgla;
using TriUgla.Parsing;

namespace DebugConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string txt = File.ReadAllText(@"text.txt");
            //Console.WriteLine(txt);

            //Console.WriteLine();

            //new Scanner(txt).ReadAll();

            //Console.WriteLine();

            //Console.WriteLine(Executor.Run(txt)); ;

            Console.WriteLine(Mat3.C(Math.PI / 2) * Mat3.B(Math.PI / 3) * Mat3.C(-Math.PI / 2)); ;

            Console.WriteLine();

            Console.WriteLine(Mat3.C(-Math.PI / 2));
        }

        public readonly struct Vec3
        {
            public readonly double x, y, z;

            public Vec3(double x, double y, double z)
            {
                this.x = x; this.y = y; this.z = z;   
            }

            public override string ToString()
            {
                return $"{x} {y} {z}";
            }
        }

        public readonly struct Mat3
        {
            public readonly double m11, m12, m13;
            public readonly double m21, m22, m23;
            public readonly double m31, m32, m33;

            public Mat3(
                double m11, double m12, double m13,
                double m21, double m22, double m23,
                double m31, double m32, double m33)
            {
                this.m11 = m11; this.m12 = m12; this.m13 = m13;
                this.m21 = m21; this.m22 = m22; this.m23 = m23;
                this.m31 = m31; this.m32 = m32; this.m33 = m33;
            }

            public static Mat3 operator *(Mat3 a, Mat3 b)
            {
                return Multiply(in a, in b);
            }

            public static Mat3 Multiply(in Mat3 a, in Mat3 b)
            {
                double b11 = b.m11, b21 = b.m21, b31 = b.m31;
                double b12 = b.m12, b22 = b.m22, b32 = b.m32;
                double b13 = b.m13, b23 = b.m23, b33 = b.m33;

                double c11 = a.m11 * b11 + a.m12 * b21 + a.m13 * b31;
                double c12 = a.m11 * b12 + a.m12 * b22 + a.m13 * b32;
                double c13 = a.m11 * b13 + a.m12 * b23 + a.m13 * b33;

                double c21 = a.m21 * b11 + a.m22 * b21 + a.m23 * b31;
                double c22 = a.m21 * b12 + a.m22 * b22 + a.m23 * b32;
                double c23 = a.m21 * b13 + a.m22 * b23 + a.m23 * b33;

                double c31 = a.m31 * b11 + a.m32 * b21 + a.m33 * b31;
                double c32 = a.m31 * b12 + a.m32 * b22 + a.m33 * b32;
                double c33 = a.m31 * b13 + a.m32 * b23 + a.m33 * b33;

                return new Mat3(c11, c12, c13,
                                c21, c22, c23,
                                c31, c32, c33);
            }

            public static Vec3 Multiply(in Mat3 m, in Vec3 v)
            {
                return new Vec3(
                    m.m11 * v.x + m.m12 * v.y + m.m13 * v.z,
                    m.m21 * v.x + m.m22 * v.y + m.m23 * v.z,
                    m.m31 * v.x + m.m32 * v.y + m.m33 * v.z);
            }

            public static Mat3 A(double alpha)
            {
                double sin = Math.Sin(alpha);
                double cos = Math.Cos(alpha);

                return new Mat3(
                    cos, sin, 0,
                    -sin, cos, 0,
                    0, 0, 1);
            }

            public static Mat3 B(double alpha)
            {
                double sin = Math.Sin(alpha);
                double cos = Math.Cos(alpha);

                return new Mat3(
                    cos, 0, -sin,
                    0, 1, 0,
                    sin, 0, cos);
            }

            public static Mat3 C(double alpha)
            {
                double sin = Math.Sin(alpha);
                double cos = Math.Cos(alpha);

                return new Mat3(
                    1, 0,0,
                    0, cos, -sin,
                    0, sin, cos);
            }

         

            public override string ToString()
            {
                return 
                    $"{m11} {m12} {m13}\n" +
                    $"{m21} {m22} {m23}\n" +
                    $"{m31} {m32} {m33}";
            }
        }
    }
}
