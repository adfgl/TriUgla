using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TriUgla.Parsing.Compiling
{
    [DebuggerDisplay("{ToString()}")]
    public readonly struct TuValue 
    {
        public static readonly TuValue Nothing = new TuValue(EDataType.Nothing, double.NaN, null);

        public readonly EDataType type;
        readonly double numeric;
        readonly TuObject? obj = null;

        TuValue(EDataType type, double numeric, TuObject? obj)
        {
            this.type = type;
            this.numeric = numeric;
            this.obj = obj;
        }

        public TuValue(bool b)
        {
            type = EDataType.Numeric;
            numeric = b ? 1 : 0;
        }

        public TuValue(int n)
        {
            type = EDataType.Numeric;
            numeric = n;
        }

        public TuValue(double n)
        {
            type = EDataType.Numeric;
            numeric = n;
        }

        public TuValue(string s)
        {
            type = EDataType.String;
            obj = new TuText(s);
        }

        public TuValue(TuRange range)
        {
            type = EDataType.Range;
            obj = range;
        }

        public TuValue(TuTuple tuple)
        {
            type = EDataType.Tuple;
            obj = tuple;
        }

        public TuValue(TuPoint point)
        {
            type = EDataType.Point;
            obj = point;
        }

        public TuPoint? AsPoint()
        {
            if (type == EDataType.Point)
            {
                return obj as TuPoint;
            }
            throw new InvalidCastException();
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

            if (type == EDataType.String || 
                type == EDataType.Range || 
                type == EDataType.Tuple || 
                type == EDataType.Point)
            {
                if (obj is null) return "";
                return obj.ToString();
            }

            if (type == EDataType.Nothing) return String.Empty;

            throw new InvalidCastException();
        }

        public bool AsBoolean()
        {
            if (type == EDataType.Numeric)
            {
                return numeric > 0;
            }

            if (type == EDataType.String || type == EDataType.Range || type == EDataType.Tuple)
            {
                return obj is not null;
            }

            throw new InvalidCastException();
        }

        public TuRange? AsRange()
        {
            if (type == EDataType.Range)
            {
                return obj as TuRange;
            }

            throw new InvalidCastException();
        }

        public TuTuple? AsTuple()
        {
            if (type == EDataType.Tuple)
            {
                return obj as TuTuple;
            }

            if (type == EDataType.Numeric)
            {
                return new TuTuple([numeric]);
            }

            if (type == EDataType.Range)
            {
                return new TuTuple(obj as TuRange);
            }

            throw new InvalidCastException();
        }

        public override string ToString()
        {
            return AsString();
        }

        public TuValue Copy()
        {
            if (type == EDataType.Numeric || obj is null)
                return this;

            var cloned = obj.Clone();
            return new TuValue(type, numeric, cloned);
        }
    }
}
