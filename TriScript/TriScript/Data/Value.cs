using System.Runtime.InteropServices;

namespace TriScript.Data
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Value : IEquatable<Value>
    {
        public static Value Nothing = new Value();

        [FieldOffset(0)] public readonly EDataType type;
        [FieldOffset(4)] public readonly double real;
        [FieldOffset(4)] public readonly int integer;
        [FieldOffset(4)] public readonly Pointer pointer;

        public override string ToString() => type switch
        {
            EDataType.Integer => integer.ToString(),
            EDataType.Real => real.ToString("G"),
            EDataType.Pointer => pointer.ToString(),
            _ => $"<{type}>"
        };

        public bool IsNothing() => type == EDataType.None;

        public double AsDouble()
        {
            if (type == EDataType.Integer) return integer;
            if (type == EDataType.Real) return real;
            return Double.NaN;
        }

        public Value(EDataType type)
        {
            this.type = type;
            switch (type)
            {
                case EDataType.Integer:
                    integer = 0;
                    break;
                case EDataType.Real:
                    real = 0.0;
                    break;
                default:
                    pointer = Pointer.Null;
                    break;
            }
        }

        public Value(bool value) : this(value ? 1 : 0) { }

        public Value(int value)
        {
            type = EDataType.Integer;
            integer = value;
        }

        public Value(double value)
        {
            type = EDataType.Real;
            real = value;
        }

        public Value(Pointer value)
        {
            type = EDataType.Pointer;
            pointer = value;
        }

        public bool Equals(Value other)
        {
            if (type == other.type)
            {
                return type switch
                {
                    EDataType.Integer => real == other.real,
                    EDataType.Real => real == other.real,
                    EDataType.Pointer => pointer.Equals(other.pointer),
                    _ => false
                };
            }
            if (type.IsNumeric() && other.type.IsNumeric())
            {
                return real == other.real;
            }
            return false;
        }

        public override bool Equals(object? obj) => obj is Value v && Equals(v);

        public override int GetHashCode() => type switch
        {
            EDataType.Integer => HashCode.Combine(type, real),
            EDataType.Real => HashCode.Combine(type, real),
            EDataType.Pointer => HashCode.Combine(type, pointer),
            _ => type.GetHashCode()
        };

        public static bool operator ==(Value a, Value b) => a.Equals(b);
        public static bool operator !=(Value a, Value b) => !a.Equals(b);
    }
}
