using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Parsing
{
    public sealed record BuiltinFunction(
      BuiltinFunctionKind Kind,
      int MinArgs,
      int MaxArgs = int.MaxValue);
}
