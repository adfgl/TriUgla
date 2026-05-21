using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Parsing
{
    public enum BuiltinFunctionKind
    {
        Print,
        Exit,

        Min,
        Max,
        Abs,
        Clamp,

        Sin,
        Cos,
        Tan,

        Asin,
        Acos,
        Atan,
        Atan2,

        Sinh,
        Cosh,
        Tanh,

        Sqrt,
        Pow,
        Exp,
        Log,
        Log10,

        Floor,
        Ceil,
        Round,
        Trunc,

        Deg,
        Rad,

        Length,
        Normalize,
        Dot,
        Cross,

        Transpose,
        Inverse,

        StrLen,
        StrLower,
        StrUpper,
        StrTrim,

        StrContains,
        StrStartsWith,
        StrEndsWith,

        StrReplace,
        StrSplit,

        SubStr,

        ToString,
        ToInt,
        ToDouble,
        ToBool,
    }
}
