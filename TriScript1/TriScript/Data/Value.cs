using System.Globalization;

namespace TriScript.Data
{
    public readonly struct Value : IEquatable<Value>
    {
        public readonly static Value Nothing = new Value(EDataType.None, Double.NaN, null);

        public readonly EDataType type;
        public readonly double real;
        public readonly Obj? obj;

        public Value(EDataType type, double real, Obj? obj)
        {
            this.type = type;
            this.real = real;
            this.obj = obj;
        }

        public Value(EDataType type) : this(type, 0, null) { }

        public Value(double real)
            : this(EDataType.Real, real, null) { }

        public Value(int integer)
            : this(EDataType.Integer, integer, null) { }

        public Value(bool value)
            : this(value ? 1 : 0) { }

        public Value(Obj obj)
            : this(obj.Type, double.NaN, obj) { }

        public static Value One(in Value val)
        {
            if (val.type == EDataType.Integer) return new Value(1);
            return new Value(0.0);
        }

        public bool Equals(Value other)
        {
            if (type.IsNumeric() && other.type.IsNumeric())
                return real.Equals(other.real);

            if (type == EDataType.None && other.type == EDataType.None)
                return true;

            if (obj is not null && other.obj is not null)
                return ReferenceEquals(obj, other.obj) || obj.Equals(other.obj);

            return false;
        }

        public override bool Equals(object? obj) => obj is Value v && Equals(v);

        public override int GetHashCode() => HashCode.Combine(type, real, obj);

        public double AsDouble()
        {
            if (type.IsNumeric()) return real;
            throw new InvalidOperationException();
        }

        public bool AsBoolean()
        {
            if (type.IsNumeric()) return real != 0;
            return obj != null;
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

        public static Value operator +(Value a)
        {
            if (a.type.IsNumeric())
            {
                return a;
            }
            throw new InvalidOperationException();
        }

        public static Value operator -(Value a)
        {
            if (a.type.IsNumeric())
            {
                double result = -a.real;
                if (a.type == EDataType.Real)
                {
                    return new Value(result);
                }
                return new Value((int)result);
            }
            throw new InvalidOperationException();
        }

        public static Value operator !(Value a)
        {
            return new Value(!a.AsBoolean());
        }

        public static Value operator +(Value a, Value b)
        {
            if (a.type == EDataType.String || b.type == EDataType.String)
            {
                return new Value(new ObjString($"{a}{b}"));
            }

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

        public static Value operator ==(Value a, Value b) => new Value(a.Equals(b));
        public static Value operator !=(Value a, Value b) => new Value(!a.Equals(b));

        public static Value operator |(Value a, Value b)
        {
            return new Value(a.AsBoolean() || b.AsBoolean());
        }

        public static Value operator &(Value a, Value b)
        {
            return new Value(a.AsBoolean() && b.AsBoolean());
        }

        public override string ToString()
        {
            switch (type)
            {
                case EDataType.Integer:
                    return ((int)real).ToString();

                case EDataType.Real:
                    return real % 1 == 0
                        ? real.ToString("0.0", CultureInfo.InvariantCulture)
                        : real.ToString("G", CultureInfo.InvariantCulture);

                default:
                    return obj is null ? "<nothing>" : obj.ToString()!;
            }
        }
    }
}
