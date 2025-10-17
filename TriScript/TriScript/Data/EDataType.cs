using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TriScript.Data
{
    [Flags]
    public enum EDataType : byte
    {
        None = 0,
        Integer = 1 << 0,
        Float = 1 << 1,
        Pointer = 1 << 2,

        String = 1 << 3,
        List = 1 << 4,

        Numeric = Integer | Float,
        Any = Integer | Float | String,
        Object = String | List,
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
        public static bool IsReal(this EDataType type) => (type & EDataType.Float) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsText(this EDataType type) => (type & EDataType.String) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAny(this EDataType type) => type == EDataType.Any;
    }
}
