using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Script.Data
{
    public readonly record struct OrientedCurveId(
    CurveId CurveId,
    bool Reversed)
    {
        public override string ToString()
        {
            return Reversed
                ? $"-{CurveId.Value}"
                : CurveId.Value.ToString();
        }

        public static OrientedCurveId FromSigned(int id)
        {
            return id < 0
                ? new OrientedCurveId(new CurveId(-id), true)
                : new OrientedCurveId(new CurveId(id), false);
        }
    }
}
