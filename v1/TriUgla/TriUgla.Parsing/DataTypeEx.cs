using System.Runtime.CompilerServices;
using TriUgla.Parsing.Data;

namespace TriUgla.Parsing
{
    public static class DataTypeEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(this EDataType type)
        => (type & EDataType.Numeric) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(this EDataType type)
            => (type & EDataType.Integer) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReal(this EDataType type)
            => (type & EDataType.Real) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsText(this EDataType type)
            => (type & EDataType.Text) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIterable(this EDataType type)
            => (type & EDataType.Iterable) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsScalar(this EDataType type)
            => (type & EDataType.Scalar) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAny(this EDataType type)
            => type == EDataType.Any;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNothing(this EDataType type)
            => type == EDataType.Nothing;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible(EDataType left, EDataType right)
            => (left & right) != 0 || left == EDataType.Any || right == EDataType.Any;

    }
}
