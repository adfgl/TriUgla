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
        public bool AsBoolean()
        {
            if (type == EDataType.Real || type == EDataType.Integer) return numeric > 0;
            if (obj is not null) return true;
            throw new InvalidCastException();
        }

        public TuValue(int n)
        {
            type = EDataType.Integer;
            numeric = n;
            obj = null;
        }
        public int AsInteger()
        {
            if (type == EDataType.Integer) return (int)numeric;
            throw new InvalidCastException();
        }

        public TuValue(double n)
        {
            type = EDataType.Real;
            numeric = n;
            obj = null;
        }
        public double AsNumeric()
        {
            if (type == EDataType.Real || type == EDataType.Integer) return numeric;
            throw new InvalidCastException();
        }

        public TuValue(string s)
        {
            type = EDataType.Text;
            obj = new TuText(s);
            numeric = double.NaN;
        }
        public TuText AsText()
        {
            if (type == EDataType.Text && obj is TuText t) return t;
            throw new InvalidCastException();
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
        public TuRange AsRange()
        {
            if (type == EDataType.Range && obj is TuRange r) return r;
            throw new InvalidCastException();
        }

        public TuValue(TuTuple tuple)
        {
            type = EDataType.Tuple;
            obj = tuple;
            numeric = double.NaN;
        }
        public TuTuple AsTuple()
        {
            if (type == EDataType.Tuple && obj is TuTuple t) return t;
            if (type.IsNumeric())
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
            if (type == EDataType.Text)
            {
                TuTuple tpl = new TuTuple();
                foreach (char dbl in AsText().Content)
                {
                    tpl.Add(new TuValue($"{dbl}"));
                }
                return tpl;
            }
            throw new InvalidCastException();
        }

        public TuValue(TuRef reference)
        {
            type = EDataType.Reference;
            obj = reference;
        }
        public TuRef AsRef()
        {
            if (type == EDataType.Reference && obj is TuRef t) return t;
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

        

     

   

        public TuValue Copy()
        {
            if (type == EDataType.Real || obj is null)
                return this;
            return new TuValue(type, numeric, obj.Clone());
        }

        public bool Equals(TuValue other)
        {
            if (type.IsNumeric() && other.type.IsNumeric())
            {
                return numeric == other.numeric;
            }
            if (type != other.type) return false;
            return Equals(obj, other.obj);
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

        static TuValue NumericOp(TuValue left, TuValue right, Func<double, double, double> op)
        {
            if (!left.type.IsNumeric() || !right.type.IsNumeric())
                throw new InvalidOperationException();

            double result = op(left.AsNumeric(), right.AsNumeric());
            return (left.type == EDataType.Real || right.type == EDataType.Real)
                ? new TuValue(result)
                : new TuValue((int)result);
        }

        static TuValue BooleanOp(TuValue left, TuValue right, Func<double, double, bool> op)
        {
            if (!left.type.IsNumeric() || !right.type.IsNumeric())
                throw new InvalidOperationException();
            return new TuValue(op(left.AsNumeric(), right.AsNumeric()));
        }

        public static TuValue operator +(TuValue left, TuValue right)
        {
            if (left.type == EDataType.Text || right.type == EDataType.Text)
                return new TuValue(left.AsString() + right.AsString());
            return NumericOp(left, right, (a, b) => a + b);
        }

        public static TuValue operator -(TuValue left, TuValue right)
            => NumericOp(left, right, (a, b) => a - b);

        public static TuValue operator *(TuValue left, TuValue right)
            => NumericOp(left, right, (a, b) => a * b);

        public static TuValue operator /(TuValue left, TuValue right)
            => NumericOp(left, right, (a, b) => a / b);

        public static TuValue operator %(TuValue left, TuValue right)
            => NumericOp(left, right, (a, b) => a % b);

        public static TuValue operator ^(TuValue left, TuValue right)
            => NumericOp(left, right, Math.Pow);

        public static TuValue operator <(TuValue left, TuValue right)
            => BooleanOp(left, right, (a, b) => a < b);

        public static TuValue operator >(TuValue left, TuValue right)
            => BooleanOp(left, right, (a, b) => a > b);

        public static TuValue operator <=(TuValue left, TuValue right)
           => BooleanOp(left, right, (a, b) => a <= b);

        public static TuValue operator >=(TuValue left, TuValue right)
            => BooleanOp(left, right, (a, b) => a >= b);

        public static TuValue operator -(TuValue value)
        {
            if (value.type.IsNumeric())
            {
                double result = -value.AsNumeric();
                if (value.type == EDataType.Numeric)
                {
                    return new TuValue(result);
                }
                return new TuValue((int)result);
            }
            throw new InvalidOperationException();
        }
    }
}
