using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TriUgla.Parsing.Scanning
{
    [DebuggerDisplay("{ToString()}")]
    public struct Value : IEquatable<Value>, IComparable<Value>
    {
        const NumberStyles NumStyles = NumberStyles.Float | NumberStyles.AllowThousands;
        static readonly IFormatProvider Inv = CultureInfo.InvariantCulture;

        public static readonly Value Nothing = new Value(EDataType.None, double.NaN, null);

        public readonly EDataType type;
        public readonly double numeric;
        public readonly string? text;

        public Value(EDataType type, double numeric, string? text)
        {
            this.type = type;
            this.numeric = numeric;
            this.text = text;
        }

        public Value(bool b) : this(EDataType.Numeric, b ? 1d : 0d, null) { }
        public Value(int n) : this(EDataType.Numeric, n, null) { }
        public Value(double n) : this(EDataType.Numeric, n, null) { }
        public Value(string s) : this(EDataType.String, double.NaN, s) { }

        public override string ToString() => type switch
        {
            EDataType.Numeric => numeric.ToString(Inv),
            EDataType.String => text ?? "",
            _ => "<none>"
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsText(in Value v) => v.type switch
        {
            EDataType.String => v.text ?? "",
            EDataType.Numeric => v.numeric.ToString(Inv),
            _ => ""
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryNum(in Value v, out double x)
        {
            if (v.type == EDataType.Numeric) { x = v.numeric; return true; }
            if (v.type == EDataType.String && v.text is not null)
                return double.TryParse(v.text, NumStyles, Inv, out x);
            x = 0; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Truthy(in Value v) =>
            v.type == EDataType.Numeric ? v.numeric != 0 :
            v.type == EDataType.String ? !string.IsNullOrEmpty(v.text) :
            false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Value other)
        {
            if (type == EDataType.None || other.type == EDataType.None)
                return type == other.type;

            if (type == EDataType.Numeric && other.type == EDataType.Numeric)
                return numeric.Equals(other.numeric);

            if (type == EDataType.String && other.type == EDataType.String)
                return string.Equals(text, other.text, StringComparison.Ordinal);

            return string.Equals(AsText(this), AsText(other), StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => obj is Value v && Equals(v);

        public override int GetHashCode()
        {
            if (type == EDataType.None) return 0;
            return StringComparer.Ordinal.GetHashCode(AsText(this));
        }

        public int CompareTo(Value other)
        {
            if (type == EDataType.None && other.type == EDataType.None) return 0;
            if (type == EDataType.None) return -1;
            if (other.type == EDataType.None) return 1;

            if (type == EDataType.Numeric && other.type == EDataType.Numeric)
                return numeric.CompareTo(other.numeric);

            if (type == EDataType.String && other.type == EDataType.String)
                return string.Compare(text ?? "", other.text ?? "", StringComparison.Ordinal);
            return string.Compare(AsText(this), AsText(other), StringComparison.Ordinal);
        }
    }
}
