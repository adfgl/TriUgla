namespace TriUgla.Parsing.Data
{
    [Flags]
    public enum EDataType : byte
    {
        Nothing = 0,

        Integer = 1 << 0,
        Real = 1 << 1,
        Text = 1 << 2,
        Range = 1 << 3,
        Tuple = 1 << 4,
        Reference = 1 << 5,

        Numeric = Integer | Real,
        Scalar = Numeric | Text,
        Iterable = Range | Tuple,

        Any = Integer | Real | Text | Range | Tuple
    }
}
