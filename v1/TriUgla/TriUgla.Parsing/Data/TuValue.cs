using System.Globalization;

namespace TriUgla.Parsing.Data
{
    public readonly struct TuValue : IEquatable<TuValue>
    {
        public static readonly TuValue Nothing = new TuValue(EDataType.Nothing, double.NaN, null);

        public readonly EDataType type;
        readonly double numeric;
        readonly TuObject? obj;

        public static bool Compatible(EDataType t1, EDataType t2)
        {
            if (t2 == EDataType.Nothing) return false;
            return t1 == EDataType.Nothing || t1 == t2 || (t1 == EDataType.Real && t2 == EDataType.Integer);
        }


        TuValue(EDataType type, double numeric, TuObject? obj)
        {
            this.type = type;
            this.numeric = numeric;
            this.obj = obj;
        }

        public TuValue(bool b)
        {
            type = EDataType.Real;
            numeric = b ? 1 : 0;
            obj = null;
        }

        public TuValue(int n)
        {
            type = EDataType.Integer;
            numeric = n;
            obj = null;
        }

        public TuValue(double n)
        {
            type = EDataType.Real;
            numeric = n;
            obj = null;
        }

        public TuValue(string s)
        {
            type = EDataType.Text;
            obj = new TuText(s);
            numeric = double.NaN;
        }

        public TuValue(TuText text)
        {
            type = EDataType.Text;
            obj = text;
            numeric = double.NaN;
        }

        public TuValue(TuRange range)
        {
            type = EDataType.Range;
            obj = range;
            numeric = double.NaN;
        }

        public TuValue(TuTuple tuple)
        {
            type = EDataType.Tuple;
            obj = tuple;
            numeric = double.NaN;
        }

        public double AsNumeric()
        {
            if (type == EDataType.Real || type == EDataType.Integer) return numeric;
            throw new InvalidCastException();
        }

        public int AsInteger()
        {
            if (type == EDataType.Integer) return (int)numeric;
            throw new InvalidCastException();
        }

        public TuText AsText()
        {
            if (type == EDataType.Text && obj is TuText t) return t;
            throw new InvalidCastException();
        }

        public override string ToString()
        {
            return $"[{type}] {AsString()}";
        }

        public string AsString()
        {
            if (type == EDataType.Real) return numeric.ToString("0.0#################", CultureInfo.InvariantCulture);
            if (type == EDataType.Integer) return ((int)numeric).ToString();
            if (obj is not null) return obj.ToString() ?? string.Empty;
            if (type == EDataType.Nothing) return string.Empty;
            throw new InvalidCastException();
        }

        public bool AsBoolean()
        {
            if (type == EDataType.Real || type == EDataType.Integer) return numeric > 0;
            if (obj is not null) return true;
            throw new InvalidCastException();
        }

        public TuRange AsRange()
        {
            if (type == EDataType.Range && obj is TuRange r) return r;
            throw new InvalidCastException();
        }

        public TuTuple AsTuple()
        {
            if (type == EDataType.Tuple && obj is TuTuple t) return t;
            if (type == EDataType.Real)
            {
                TuTuple tpl = new TuTuple();
                tpl.Add(this);
                return tpl;
            }
            if (type == EDataType.Range && obj is TuRange range)
            {
                TuTuple tpl = new TuTuple();
                foreach (var dbl in range)
                {
                    tpl.Add(dbl);
                }
                return tpl;
            }
            throw new InvalidCastException();
        }

        public TuValue Copy()
        {
            if (type == EDataType.Real || obj is null)
                return this;
            return new TuValue(type, numeric, obj.Clone());
        }

        public bool Equals(TuValue other)
        {
            if (type != other.type) return false;

            return type switch
            {
                EDataType.Real => numeric.Equals(other.numeric),
                EDataType.Text or EDataType.Range or EDataType.Tuple
                    => Equals(obj, other.obj),
                EDataType.Nothing => true,
                _ => false
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is TuValue value && Equals(value);
        }

        public override int GetHashCode()
        {
            return type switch
            {
                EDataType.Real => HashCode.Combine(type, numeric),
                _ => HashCode.Combine(type, obj),
            };
        }

        public static bool operator ==(TuValue left, TuValue right) => left.Equals(right);
        public static bool operator !=(TuValue left, TuValue right) => !left.Equals(right);

        public static TuValue operator +(TuValue left, TuValue right)
        {
            if (left.type == EDataType.Text || right.type == EDataType.Text)
            {
                return new TuValue(left.AsString() +  right.AsString());
            }

            if (left.type.IsNumeric() && right.type.IsNumeric())
            {
                double result = left.AsNumeric() + right.AsNumeric();
                if (left.type == EDataType.Numeric || right.type == EDataType.Numeric)
                {
                    return new TuValue(result);
                }
                return new TuValue((int)result);
            }

            throw new InvalidOperationException();
        }

        public static TuValue operator -(TuValue value)
        {
            switch (value.type)
            {
                case EDataType.Real:
                    return new TuValue(-value.AsNumeric());

                case EDataType.Integer:
                    return new TuValue(-(int)value.AsNumeric());

                default:
                    throw new InvalidOperationException(
                        $"Unary '-' is not defined for type {value.type}.");
            }
        }

    }
}
