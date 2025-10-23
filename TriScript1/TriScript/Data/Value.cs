
using TriScript.UnitHandling;

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
    }
}
