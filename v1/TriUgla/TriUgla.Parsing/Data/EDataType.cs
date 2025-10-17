namespace TriUgla.Parsing.Data
{
    [Flags]
    public enum EDataType : byte
    {
        Nothing = 0,

        Integer = 1 << 0,
        Float = 1 << 1,
        String = 1 << 2,
        Range = 1 << 3,
        List = 1 << 4,
        Reference = 1 << 5,

        Numeric = Integer | Float,
        Scalar = Numeric | String,
        Iterable = Range | List,

        Any = Integer | Float | String | Range | List
    }
}
