using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Parsing
{
    public static class Builtins
    {
        public static readonly Dictionary<string, BuiltinFunction> Functions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["print"] = new(BuiltinFunctionKind.Print, 0),
                ["exit"] = new(BuiltinFunctionKind.Exit, 1, 1),

                ["Min"] = new(BuiltinFunctionKind.Min, 2),
                ["Max"] = new(BuiltinFunctionKind.Max, 2),
                ["Abs"] = new(BuiltinFunctionKind.Abs, 1, 1),
                ["Clamp"] = new(BuiltinFunctionKind.Clamp, 3, 3),

                ["Sin"] = new(BuiltinFunctionKind.Sin, 1, 1),
                ["Cos"] = new(BuiltinFunctionKind.Cos, 1, 1),
                ["Tan"] = new(BuiltinFunctionKind.Tan, 1, 1),

                ["Asin"] = new(BuiltinFunctionKind.Asin, 1, 1),
                ["Acos"] = new(BuiltinFunctionKind.Acos, 1, 1),
                ["Atan"] = new(BuiltinFunctionKind.Atan, 1, 1),
                ["Atan2"] = new(BuiltinFunctionKind.Atan2, 2, 2),

                ["Sinh"] = new(BuiltinFunctionKind.Sinh, 1, 1),
                ["Cosh"] = new(BuiltinFunctionKind.Cosh, 1, 1),
                ["Tanh"] = new(BuiltinFunctionKind.Tanh, 1, 1),

                ["Sqrt"] = new(BuiltinFunctionKind.Sqrt, 1, 1),
                ["Pow"] = new(BuiltinFunctionKind.Pow, 2, 2),

                ["Exp"] = new(BuiltinFunctionKind.Exp, 1, 1),
                ["Log"] = new(BuiltinFunctionKind.Log, 1, 2),
                ["Log10"] = new(BuiltinFunctionKind.Log10, 1, 1),

                ["Floor"] = new(BuiltinFunctionKind.Floor, 1, 1),
                ["Ceil"] = new(BuiltinFunctionKind.Ceil, 1, 1),
                ["Round"] = new(BuiltinFunctionKind.Round, 1, 2),
                ["Trunc"] = new(BuiltinFunctionKind.Trunc, 1, 1),

                ["Deg"] = new(BuiltinFunctionKind.Deg, 1, 1),
                ["Rad"] = new(BuiltinFunctionKind.Rad, 1, 1),

                ["Length"] = new(BuiltinFunctionKind.Length, 1, 1),
                ["Normalize"] = new(BuiltinFunctionKind.Normalize, 1, 1),
                ["Dot"] = new(BuiltinFunctionKind.Dot, 2, 2),
                ["Cross"] = new(BuiltinFunctionKind.Cross, 2, 2),

                ["Transpose"] = new(BuiltinFunctionKind.Transpose, 1, 1),
                ["Inverse"] = new(BuiltinFunctionKind.Inverse, 1, 1),

                ["StrLen"] = new(BuiltinFunctionKind.StrLen, 1, 1),
                ["StrLower"] = new(BuiltinFunctionKind.StrLower, 1, 1),
                ["StrUpper"] = new(BuiltinFunctionKind.StrUpper, 1, 1),
                ["StrTrim"] = new(BuiltinFunctionKind.StrTrim, 1, 1),

                ["StrContains"] = new(BuiltinFunctionKind.StrContains, 2, 2),
                ["StrStartsWith"] = new(BuiltinFunctionKind.StrStartsWith, 2, 2),
                ["StrEndsWith"] = new(BuiltinFunctionKind.StrEndsWith, 2, 2),

                ["StrReplace"] = new(BuiltinFunctionKind.StrReplace, 3, 3),
                ["StrSplit"] = new(BuiltinFunctionKind.StrSplit, 2, 2),

                ["SubStr"] = new(BuiltinFunctionKind.SubStr, 2, 3),

                ["ToString"] = new(BuiltinFunctionKind.ToString, 1, 1),
                ["ToInt"] = new(BuiltinFunctionKind.ToInt, 1, 1),
                ["ToDouble"] = new(BuiltinFunctionKind.ToDouble, 1, 1),
                ["ToBool"] = new(BuiltinFunctionKind.ToBool, 1, 1),
            };

        public static bool TryGet(string name, out BuiltinFunction function)
            => Functions.TryGetValue(name, out function);
    }
}
