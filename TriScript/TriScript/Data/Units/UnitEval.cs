using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriScript.Data.Units
{
    public readonly struct UnitEval
    {
        public readonly double ScaleToMeter; // multiply numeric by this to get SI (meters^Lexp)
        public readonly Dimension Dim;       // L^k

        public UnitEval(double scaleToMeter, Dimension dim)
        {
            ScaleToMeter = scaleToMeter;
            Dim = dim;
        }

        public bool IsValid => !double.IsNaN(ScaleToMeter) && !double.IsInfinity(ScaleToMeter);
        public override string ToString() => $"scale={ScaleToMeter}, dim={Dim}";
    }
}
