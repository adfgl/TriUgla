using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using TriUgla.Parsing.Compiling.RuntimeObjects;

namespace TriUgla.Parsing.Compiling
{
    [DebuggerDisplay("{ToString()}")]
    public struct Value
    {
        public static readonly Value Nothing = new Value();

        public readonly EDataType type;
        readonly double numeric;
        readonly Object? obj = null;

        public Value(bool b)
        {
            this.type = EDataType.Numeric;
            this.numeric = b ? 1 : 0;
        }

        public Value(int n)
        {
            this.type = EDataType.Numeric;
            this.numeric = n;
        }

        public Value(double n)
        {
            this.type = EDataType.Numeric;
            this.numeric = n;
        }

        public Value(string s)
        {
            this.type = EDataType.String;
            this.obj = new Text(s);
        }

        public Value(Range range)
        {
            this.type = EDataType.Range;
            this.obj = range;
        }

        public double AsNumeric()
        {
            if (type == EDataType.Numeric)
            {
                return numeric;
            }
            throw new InvalidCastException();
        }

        public string AsString()
        {
            if (type == EDataType.Numeric)
            {
                return numeric.ToString(CultureInfo.InvariantCulture);
            }

            if (type == EDataType.String || type == EDataType.Range)
            {
                if (obj is null) return "";
                return obj.ToString();
            }

            if (type == EDataType.None) return "";

            throw new InvalidCastException();
        }

        public bool AsBoolean()
        {
            if (type == EDataType.Numeric)
            {
                return numeric > 0;
            }

            if (type == EDataType.String || type == EDataType.Range)
            {
                return obj is not null;
            }

            throw new InvalidCastException();
        }

        public Range? AsRange()
        {
            if (type == EDataType.Range)
            {
                return obj as Range;
            }

            throw new InvalidCastException();
        }

        public override string ToString()
        {
            return AsString();
        }
    }
}
