using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriUgla.Parsing.Data
{
    public sealed class Matrix
    {
        private double[] _data;
        public int Rows { get; private set; }
        public int Cols { get; private set; }
        public int Count => _data.Length;

        public Matrix(int rows, int cols)
        {
            if (rows <= 0 || cols <= 0)
                throw new ArgumentException("Matrix dimensions must be positive.");
            Rows = rows;
            Cols = cols;
            _data = new double[rows * cols];
        }

        public Matrix(double[,] src)
        {
            Rows = src.GetLength(0);
            Cols = src.GetLength(1);
            _data = new double[Rows * Cols];
            Buffer.BlockCopy(src, 0, _data, 0, sizeof(double) * _data.Length);
        }

        public double this[int row, int col]
        {
            get
            {
                if ((uint)row >= (uint)Rows || (uint)col >= (uint)Cols)
                    throw new IndexOutOfRangeException();
                return _data[row * Cols + col];
            }
            set
            {
                if ((uint)row >= (uint)Rows || (uint)col >= (uint)Cols)
                    throw new IndexOutOfRangeException();
                _data[row * Cols + col] = value;
            }
        }

        public void Fill(Func<int, int, double> generator)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _data[r * Cols + c] = generator(r, c);
        }

        public Matrix Clone()
        {
            var m = new Matrix(Rows, Cols);
            Array.Copy(_data, m._data, _data.Length);
            return m;
        }

        // Resize (preserving overlapping region)
        public void Resize(int newRows, int newCols)
        {
            if (newRows <= 0 || newCols <= 0)
                throw new ArgumentException("Matrix dimensions must be positive.");

            var newData = new double[newRows * newCols];
            int minR = Math.Min(Rows, newRows);
            int minC = Math.Min(Cols, newCols);

            for (int r = 0; r < minR; r++)
                Array.Copy(_data, r * Cols, newData, r * newCols, minC);

            _data = newData;
            Rows = newRows;
            Cols = newCols;
        }

        // ---- Basic operations ----

        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Cols != b.Cols)
                throw new ArgumentException("Matrix sizes must match.");
            var r = new Matrix(a.Rows, a.Cols);
            for (int i = 0; i < a._data.Length; i++)
                r._data[i] = a._data[i] + b._data[i];
            return r;
        }

        public static Matrix operator -(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Cols != b.Cols)
                throw new ArgumentException("Matrix sizes must match.");
            var r = new Matrix(a.Rows, a.Cols);
            for (int i = 0; i < a._data.Length; i++)
                r._data[i] = a._data[i] - b._data[i];
            return r;
        }

        public static Matrix operator *(Matrix a, double s)
        {
            var r = new Matrix(a.Rows, a.Cols);
            for (int i = 0; i < a._data.Length; i++)
                r._data[i] = a._data[i] * s;
            return r;
        }

        public static Matrix operator /(Matrix a, double s)
        {
            var r = new Matrix(a.Rows, a.Cols);
            for (int i = 0; i < a._data.Length; i++)
                r._data[i] = a._data[i] / s;
            return r;
        }

        // True matrix product (A * B)
        public static Matrix Dot(Matrix A, Matrix B)
        {
            if (A.Cols != B.Rows)
                throw new ArgumentException("Inner dimensions must match for multiplication.");

            var R = new Matrix(A.Rows, B.Cols);
            for (int i = 0; i < A.Rows; i++)
            {
                for (int k = 0; k < A.Cols; k++)
                {
                    double a = A[i, k];
                    int rowBase = i * B.Cols;
                    int colBase = k * B.Cols;
                    for (int j = 0; j < B.Cols; j++)
                        R._data[rowBase + j] += a * B._data[colBase + j];
                }
            }
            return R;
        }

        public override string ToString()
            => $"Matrix[{Rows}×{Cols}]";
    }

}
