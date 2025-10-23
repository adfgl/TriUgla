
using TriScript.UnitHandling;

namespace TriScript.Data
{
    public readonly struct Value
    {
        public readonly static Value Nothing = new Value(EDataType.None, Double.NaN, DimEval.None);

        public readonly EDataType type;
        public readonly double real;
        public readonly DimEval dimension;

        public Value(EDataType type, double real, DimEval dimension)
        {
            this.type = type;
            this.real = real;
            this.dimension = dimension;
        }

        public Value(double real, DimEval dimension)
            : this(EDataType.Real, real, dimension) { }

        public Value(int integer, DimEval dimension)
            : this(EDataType.Integer, integer, dimension) { }

        public Value(bool value, DimEval dimension)
            : this(value ? 1 : 0, dimension) { }

        public double AsDouble()
        {
            if (type.IsNumeric()) return real;
            return Double.NaN;
        }
    }
}
