using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TriScript.Data
{
    [Flags]
    public enum EDataType : ushort
    {
        None = 0,
        Integer = 1 << 0,
        Real = 1 << 1,
        Pointer = 1 << 2,
        Boolean = 1 << 3,
        Character = 1 << 4,

        String = 1 << 5,
        List = 1 << 6,
        Tuple = 1 << 7,
        Range = 1 << 8,
        Matrix  = 1 << 9,

        Numeric = Integer | Real,
        Any = Integer | Real | String | Boolean | Character | List | Tuple | Range | Matrix,
        Object = String | List | Tuple | Range | Matrix
    }

    public static class DataTypeEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(this EDataType type) => (type & EDataType.Numeric) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsObject(this EDataType type) => (type & EDataType.Object) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(this EDataType type) => (type & EDataType.Integer) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReal(this EDataType type) => (type & EDataType.Real) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsText(this EDataType type) => (type & EDataType.String) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAny(this EDataType type) => type == EDataType.Any;
    }
}
