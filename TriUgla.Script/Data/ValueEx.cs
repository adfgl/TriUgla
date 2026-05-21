using TriUgla.Script.Data.Collections;

namespace TriUgla.Script.Data
{
    public static class ValueEx
    {
        public static Value Number(DataKind kind, double value)
        {
            return kind == DataKind.Integer
                ? new Value((int)value)
                : new Value(value);
        }

        public static Value Add(this Value a, Value b)
        {
            if (a.IsString || b.IsString)
                return Value.FromString(a.ToString() + b.ToString());

            Numeric(a, b, out DataKind kind, out double x, out double y);

            return Number(kind, x + y);
        }

        public static Value Subtract(this Value a, Value b)
        {
            Numeric(a, b, out DataKind kind, out double x, out double y);
            return Number(kind, x - y);
        }

        public static Value Multiply(this Value a, Value b)
        {
            Numeric(a, b, out DataKind kind, out double x, out double y);
            return Number(kind, x * y);
        }

        public static Value Divide(this Value a, Value b)
        {
            Numeric(a, b, out _, out double x, out double y);

            if (y == 0.0)
                throw new DivideByZeroException();

            return new Value(x / y);
        }

        public static Value Modulo(this Value a, Value b)
        {
            Numeric(a, b, out DataKind kind, out double x, out double y);

            if (y == 0.0)
                throw new DivideByZeroException();

            return kind == DataKind.Integer
                ? new Value((int)x % (int)y)
                : new Value(x % y);
        }

        public static Value Power(this Value a, Value b)
        {
            return new Value(Math.Pow(a.AsDouble(), b.AsDouble()));
        }

        public static Value Negate(this Value value)
        {
            return value.Kind switch
            {
                DataKind.Integer => new Value(-value.AsInt()),
                DataKind.Double => new Value(-value.AsDouble()),
                _ => throw new InvalidOperationException($"Cannot negate {value.Kind}.")
            };
        }

        public static Value Not(this Value value)
        {
            return new Value(!value.AsBool());
        }

        public static Value EqualTo(this Value a, Value b)
        {
            if (a.IsNumber && b.IsNumber)
                return new Value(Math.Abs(a.AsDouble() - b.AsDouble()) < 1e-12);

            if (a.Kind != b.Kind)
                return new Value(false);

            return a.Kind switch
            {
                DataKind.Undefined => new Value(true),
                DataKind.Boolean => new Value(a.AsBool() == b.AsBool()),
                DataKind.String => new Value(a.AsString() == b.AsString()),
                DataKind.List => new Value(ReferenceEquals(a.Object, b.Object)),
                _ => new Value(ReferenceEquals(a.Object, b.Object))
            };
        }

        public static Value NotEqualTo(this Value a, Value b)
        {
            return a.EqualTo(b).Not();
        }

        public static Value LessThan(this Value a, Value b)
        {
            return new Value(a.AsDouble() < b.AsDouble());
        }

        public static Value LessEqual(this Value a, Value b)
        {
            return new Value(a.AsDouble() <= b.AsDouble());
        }

        public static Value GreaterThan(this Value a, Value b)
        {
            return new Value(a.AsDouble() > b.AsDouble());
        }

        public static Value GreaterEqual(this Value a, Value b)
        {
            return new Value(a.AsDouble() >= b.AsDouble());
        }

        public static Value And(this Value a, Value b)
        {
            return new Value(a.AsBool() && b.AsBool());
        }

        public static Value Or(this Value a, Value b)
        {
            return new Value(a.AsBool() || b.AsBool());
        }

        public static List<Value> ToFlatList(this Value value)
        {
            if (value.Object is ObjList list)
                return [.. list.Values];

            if (value.Object is ObjRange range)
                return [.. range.Enumerate()];

            throw new InvalidOperationException(
                $"Expected List or Range, got {value.Kind}.");
        }

        public static double[] ToVector(this Value value)
        {
            List<Value> values = value.ToFlatList();
            double[] result = new double[values.Count];

            for (int i = 0; i < values.Count; i++)
                result[i] = values[i].AsDouble();

            return result;
        }

        public static double[,] ToMatrix(this Value value)
        {
            List<Value> rows = value.ToFlatList();

            if (rows.Count == 0)
                return new double[0, 0];

            double[][] temp = new double[rows.Count][];
            int cols = -1;

            for (int r = 0; r < rows.Count; r++)
            {
                temp[r] = rows[r].ToVector();

                if (cols < 0)
                {
                    cols = temp[r].Length;
                }
                else if (temp[r].Length != cols)
                {
                    throw new InvalidOperationException(
                        "Matrix rows must have equal length.");
                }
            }

            double[,] matrix = new double[rows.Count, cols];

            for (int r = 0; r < rows.Count; r++)
                for (int c = 0; c < cols; c++)
                    matrix[r, c] = temp[r][c];

            return matrix;
        }

        public static Value FromVector(IEnumerable<double> values)
        {
            List<Value> result = [];

            foreach (double value in values)
                result.Add(new Value(value));

            return Value.FromList(result);
        }

        public static Value FromMatrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            List<Value> result = new(rows);

            for (int r = 0; r < rows; r++)
            {
                List<Value> row = new(cols);

                for (int c = 0; c < cols; c++)
                    row.Add(new Value(matrix[r, c]));

                result.Add(Value.FromList(row));
            }

            return Value.FromList(result);
        }

        public static Value Transpose(this Value value)
        {
            double[,] a = value.ToMatrix();

            int rows = a.GetLength(0);
            int cols = a.GetLength(1);

            double[,] result = new double[cols, rows];

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    result[c, r] = a[r, c];

            return FromMatrix(result);
        }

        public static Value Inverse(this Value value)
        {
            double[,] a = value.ToMatrix();

            int n = a.GetLength(0);

            if (n != a.GetLength(1))
                throw new InvalidOperationException(
                    "Only square matrices can be inverted.");

            double[,] aug = new double[n, n * 2];

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                    aug[r, c] = a[r, c];

                aug[r, n + r] = 1.0;
            }

            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                double max = Math.Abs(aug[col, col]);

                for (int r = col + 1; r < n; r++)
                {
                    double v = Math.Abs(aug[r, col]);

                    if (v > max)
                    {
                        max = v;
                        pivot = r;
                    }
                }

                if (max < 1e-14)
                    throw new InvalidOperationException("Matrix is singular.");

                SwapRows(aug, pivot, col);

                double div = aug[col, col];

                for (int c = 0; c < n * 2; c++)
                    aug[col, c] /= div;

                for (int r = 0; r < n; r++)
                {
                    if (r == col)
                        continue;

                    double factor = aug[r, col];

                    for (int c = 0; c < n * 2; c++)
                        aug[r, c] -= factor * aug[col, c];
                }
            }

            double[,] inv = new double[n, n];

            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    inv[r, c] = aug[r, n + c];

            return FromMatrix(inv);
        }

        static void Numeric(
            Value a,
            Value b,
            out DataKind kind,
            out double x,
            out double y)
        {
            if (!a.IsNumber || !b.IsNumber)
            {
                throw new InvalidOperationException(
                    $"Expected numbers, got {a.Kind} and {b.Kind}.");
            }

            kind = a.Kind == DataKind.Double || b.Kind == DataKind.Double
                ? DataKind.Double
                : DataKind.Integer;

            x = a.AsDouble();
            y = b.AsDouble();
        }

        static void SwapRows(double[,] a, int r1, int r2)
        {
            if (r1 == r2)
                return;

            int cols = a.GetLength(1);

            for (int c = 0; c < cols; c++)
                (a[r1, c], a[r2, c]) = (a[r2, c], a[r1, c]);
        }
    }
}
