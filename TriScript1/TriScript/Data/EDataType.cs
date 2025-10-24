using System.Runtime.CompilerServices;

namespace TriScript.Data
{
    [Flags]
    public enum EDataType : ushort
    {
        None,

        Integer = 1 << 0,
        Real = 1 << 1,
        Symbol = 1 << 2,

        Numeric = Integer | Real,
        Any = Integer | Real
    }

    public static class DataTypeEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(this EDataType type) => (type & EDataType.Numeric) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(this EDataType type) => (type & EDataType.Integer) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReal(this EDataType type) => (type & EDataType.Real) != 0;

    }
}
