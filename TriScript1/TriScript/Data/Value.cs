namespace TriScript.Data
{
    public readonly struct Value
    {
        public readonly static Value Nothing = new Value(EDataType.None, Double.NaN);

        public readonly EDataType type;
        public readonly double real;

        public Value(EDataType type, double real)
        {
            this.type = type;
            this.real = real;
        }

        public Value(double real)
            : this(EDataType.Real, real) { }

        public Value(int integer)
            : this(EDataType.Integer, integer) { }

        public Value(bool value)
            : this(value ? 1 : 0) { }

        public double AsDouble()
        {
            if (type.IsNumeric()) return real;
            return Double.NaN;
        }

        public static Value Pow(in Value a, in Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                double result = Math.Pow(a.real, b.real);
                if (a.type == EDataType.Real || b.type == EDataType.Real)
                {
                    return new Value(result);
                }
                return new Value((int)result);
            }
            throw new InvalidOperationException();
        }

        public static Value operator +(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                double result = a.real + b.real;
                if (a.type == EDataType.Real || b.type == EDataType.Real)
                {
                    return new Value(result);
                }
                return new Value((int)result);
            }
            throw new InvalidOperationException();
        }

        public static Value operator -(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                double result = a.real - b.real;
                if (a.type == EDataType.Real || b.type == EDataType.Real)
                {
                    return new Value(result);
                }
                return new Value((int)result);
            }
            throw new InvalidOperationException();
        }

        public static Value operator *(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                double result = a.real * b.real;
                if (a.type == EDataType.Real || b.type == EDataType.Real)
                {
                    return new Value(result);
                }
                return new Value((int)result);
            }
            throw new InvalidOperationException();
        }

        public static Value operator /(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                double result = a.real / b.real;
                if (a.type == EDataType.Real || b.type == EDataType.Real)
                {
                    return new Value(result);
                }
                return new Value((int)result);
            }
            throw new InvalidOperationException();
        }

        public static Value operator <(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                return new Value(a.real < b.real);
            }
            throw new InvalidOperationException();
        }

        public static Value operator >(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                return new Value(a.real > b.real);
            }
            throw new InvalidOperationException();
        }

        public static Value operator <=(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                return new Value(a.real <= b.real);
            }
            throw new InvalidOperationException();
        }

        public static Value operator >=(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                return new Value(a.real >= b.real);
            }
            throw new InvalidOperationException();
        }

        public static Value operator ==(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                return new Value(a.real == b.real);
            }
            throw new InvalidOperationException();
        }

        public static Value operator !=(Value a, Value b)
        {
            if (a.type.IsNumeric() && b.type.IsNumeric())
            {
                return new Value(a.real != b.real);
            }
            throw new InvalidOperationException();
        }

        public static Value operator |(Value a, Value b)
        {
            return new Value(a.AsBoolean() || b.AsBoolean());
        }

        public static Value operator &(Value a, Value b)
        {
            return new Value(a.AsBoolean() && b.AsBoolean());
        }

        public bool AsBoolean()
        {
            if (type.IsNumeric()) return real != 0;
            throw new InvalidOperationException();
        }

        public override string ToString()
        {
            switch (type)
            {
                case EDataType.Integer:
                    return ((int)real).ToString();

                case EDataType.Real:
                    return real.ToString("G");

                default:
                    throw new NotImplementedException($"{type}");
            }
        }
    }
}
