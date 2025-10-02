using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TriUgla.Parsing
{
    [DebuggerDisplay("{ToString()}")]
    public struct Value
    {
        public static Value Nothing = new Value(EDataType.None, Double.NaN, null);

        public readonly EDataType type;
        public readonly double numeric;
        public readonly string? text;

        public Value(EDataType type, double numeric, string? text)
        {
            this.type = type;
            this.numeric = numeric;
            this.text = text;
        }

        public Value(bool boolean) : this(EDataType.Numeric, boolean ? 1 : 0, null) { }
        public Value(int number) : this(EDataType.Numeric, number, null) { }
        public Value(double number) : this(EDataType.Numeric, number, null) { }
        public Value(string text) : this(EDataType.String, Double.NaN, text) { }    

        public override string ToString() => type switch
        {
            EDataType.Numeric => numeric.ToString(),
            EDataType.String => String.IsNullOrEmpty(text) ? "<null>" : text,
            _ => "<none>"
        };
    }
}
