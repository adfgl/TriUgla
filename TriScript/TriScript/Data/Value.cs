using System.Runtime.InteropServices;

namespace TriScript.Data
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Value : IEquatable<Value>
    {
        public static Value Nothing = new Value();

        [FieldOffset(0)] public readonly EDataType type;
        [FieldOffset(4)] readonly double number;
        [FieldOffset(4)] readonly Pointer pointer;

        public override string ToString() => type switch
        {
            EDataType.Integer => ((int)number).ToString(),
            EDataType.Float => number.ToString("G"),
            EDataType.Pointer => pointer.ToString(),
            _ => $"<{type}>"
        };

        public bool IsNothing() => type == EDataType.None;

        public Value(int value)
        {
            type = EDataType.Integer;
            number = value;
        }
        public int AsInteger()
        {
            if (type.IsNumeric()) return (int)number;
            throw new InvalidCastException();
        }

        public Value(double value)
        {
            type = EDataType.Float;
            number = value;
        }
        public double AsDouble()
        {
            if (type.IsNumeric()) return number;
            throw new InvalidCastException();
        }

        public Value(bool value)
        {
            type = EDataType.Integer;
            number = value ? 1 : 0;
        }
        public bool AsBoolean()
        {
            if (type.IsNumeric()) return number != 0;
            if (type == EDataType.Pointer) return !pointer.IsNull;
            throw new InvalidCastException();
        }

        public Value(Pointer value)
        {
            type = EDataType.Pointer;
            pointer = value;
        }
        public Pointer AsPointer()
        {
            if (type == EDataType.Pointer) return pointer;
            throw new InvalidCastException();
        }

        public bool Equals(Value other)
        {
            if (type == other.type)
            {
                return type switch
                {
                    EDataType.Integer => number == other.number,
                    EDataType.Float => number == other.number,
                    EDataType.Pointer => pointer.Equals(other.pointer),
                    _ => false
                };
            }
            if (type.IsNumeric() && other.type.IsNumeric())
            {
                return number == other.number;
            }
            return false;
        }

        public override bool Equals(object? obj) => obj is Value v && Equals(v);

        public override int GetHashCode() => type switch
        {
            EDataType.Integer => HashCode.Combine(type, number),
            EDataType.Float => HashCode.Combine(type, number),
            EDataType.Pointer => HashCode.Combine(type, pointer),
            _ => type.GetHashCode()
        };

        public static bool operator ==(Value a, Value b) => a.Equals(b);
        public static bool operator !=(Value a, Value b) => !a.Equals(b);
    }
}
